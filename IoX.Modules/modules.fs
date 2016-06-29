﻿namespace IoX.Modules

open Suave
open Suave.Operators
open Suave.Files
open IoX.Suave
open Suave.EvReact
open EvReact
open System.Net
open IoX.Json

[<AbstractClass>]
type Module(name:string, title:string, description:string) =
  let configurationName = "module.conf"
  let verbs = WebPartArray()
  let isMain = name = Module.ROOT
  do
    if name = null ||
       name = "" ||
       (not(isMain) && System.Text.RegularExpressions.Regex.IsMatch(name, "[^_a-zA-Z\\-0-9]")) then
      failwith "Invalid module name"

  member this.Name = name
  member this.Title = title
  member this.Description = description

  member val Browsable = false with get, set

  member this.ModuleStaticFilesPath
    with get() =
      let sep = System.IO.Path.DirectorySeparatorChar
      if isMain then
        Module.HomePath
      else
        sprintf "%s%cstatic" this.ModuleFolder sep

  member this.ModuleFolder
    with get() =
      let sep = System.IO.Path.DirectorySeparatorChar
      if isMain then
        Module.HomePath
      else
        sprintf "%s%c%s" Module.ModulesPath sep name

  member this.DefaultConfigurationFileFullName =
    let sep = System.IO.Path.DirectorySeparatorChar
    sprintf "%s%c%s" this.ModuleFolder sep configurationName

  member this.Verbs = verbs

  member this.Handle query =
    let r = RequestErrors.FORBIDDEN "Query not supported"
    let r = if this.Browsable then browseFile this.ModuleStaticFilesPath query <|> r else r
    this.Verbs.choose() <|> r

  member this.ModuleFilter
    with get() = verbs.Filter
    and set(v) = verbs.Filter <- v 

  member this.RegisterReplyEvent<'T>(pattern:string, ?msTimeout:int) =
    let ewp, evt:WebPart*IEvent<MsgRequestEventArgs<'T>> = msgResponse(msTimeout)
    let wp = regex pattern >=> ewp
    verbs.Add(wp)
    evt

  member this.RegisterEvent<'T>(pattern:string) =
    let ewp, evt:WebPart*IEvent<MsgContext*'T> = msgReact()
    let wp = regex pattern >=> ewp
    verbs.Add(wp)
    evt
  
  member this.CreateRemoteTrigger<'T>(uri) =
    createRemoteTrigger<'T>(defaultSendJson uri)

  member this.ActivateNet<'T> (net:Expr<'T>) =
    let orchestrator : Orchestrator<'T> = Orchestrator.create()
    Utils.start0 orchestrator net

  abstract OnLoad : unit -> unit

  static member ROOT = "//"

  static member HomePath =
    let sep = System.IO.Path.DirectorySeparatorChar
    sprintf @"%s%c%s" System.Environment.CurrentDirectory sep "Home"

  static member ModulesPath =
    let sep = System.IO.Path.DirectorySeparatorChar
    sprintf @"%s%c%s" System.Environment.CurrentDirectory sep "Modules"

[<AbstractClass>]
type DriverModule(name:string, title:string, description:string) =
  inherit Module(name, title, description)
  let isMain = name = Module.ROOT
  do
    if name = null || 
       (not(isMain) 
        && (System.Text.RegularExpressions.Regex.IsMatch(name, "[^a-zA-Z\\-0-9]") 
            || name = System.String.Empty)) then
      failwith "Invalid module name"

  member this.RegisterHttpEvent (pattern:string, ?filter:WebPart, ?msTimeout:int) = 
    let wp, evt = httpResponse(msTimeout)
    let rxwp = regex pattern >=> wp
    let rwp = match filter with Some f -> f >=> rxwp | None -> rxwp
    this.Verbs.Add(rwp)
    evt
  
  member this.SendJsonMessage (data:Json, dest:System.Uri) =
    async {
      use c = new WebClient()
      c.Headers.Add("content-type", "application/json")
      let! ret = Async.AwaitTask(c.UploadStringTaskAsync(dest, (data |> toJson).ToString()))
      return ret
    }

  member this.SendREST(data:System.Collections.Generic.IDictionary<string, string option>, dest:System.Uri) =
    async {
      use c = new WebClient()
      let txt = System.Text.StringBuilder()
      txt.Append("?") |> ignore
      let mutable first = true
      for kv in data do
        let ek = System.Uri.EscapeDataString(kv.Key)
        if first then first <- false
        else txt.Append('&') |> ignore
        match kv.Value with
        | Some v -> 
          let ev = System.Uri.EscapeDataString(v)
          txt.AppendFormat("{0}={1}", ek, ev) |> ignore
        | None ->
          txt.Append(ek) |> ignore
      let u = System.Uri(sprintf "%s%s" dest.OriginalString (txt.ToString()))
      let! ret = c.AsyncDownloadString(u)
      return ret
    }

module Conf =
  type ModuleDescriptor = { Name:string; Title:string; Assembly:string; Description:string; Summary: string }

  let DefaultDescriptor = { Name = null; Title = null; Assembly = null; Description = ""; Summary = ""}

  type ConfigurationFile<'T>(fn:string, def:'T) =
    let mutable conf = def

    member this.Configuration
      with get() = conf
      and set(v) = conf <- v

    member this.Load() =
      if System.IO.File.Exists(fn) then
        conf <- IoX.Utils.readConfigurationFile(fn)

    member this.Save() =
      IoX.Utils.writeConfigurationFile fn conf
