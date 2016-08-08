namespace IoX.Modules

open EvReact
open EvReact.Expr
open IoX.Suave
open Suave
open Suave.Operators
open Suave.EvReact
open System
open System.Net

[<AttributeUsage(AttributeTargets.Class, Inherited=false)>]
type ModuleAttribute() =
  inherit Attribute()
  member val Name = "IoX module" with get, set
  member val Description = "Description missing" with get, set

type IModuleData<'C> =
  abstract member Id: string
  abstract member BaseUri: System.Uri
  abstract member Path: string
  abstract member Configuration: IoX.Utils.ConfigurationFile<'C>
  // TODO: abstract member Logger: Logging.Logger

type Module() =
  let verbs = WebPartDispatcher("relPath")
  let genericVerbs = WebPartArray()

  member val WebPart = verbs.WebPart <|> genericVerbs.WebPart
  member internal this.Register(wp) = genericVerbs.Add(wp)
  member internal this.Register(path, wp) = verbs.Add(path, wp)
  member internal this.RegisterEvent(path, (wp, evt)) =
    this.Register(path, wp)
    evt

  member this.ActivateNet net =
    let orchestrator = Orchestrator.create()
    Utils.start0 orchestrator net

  member this.RegisterReplyEvent(path, ?msTimeout) = this.RegisterEvent(path, msgResponse msTimeout)
  member this.RegisterEvent(path) = this.RegisterEvent(path, msgReact ())

  member this.Root with set wp = this.Register("", wp)
  member val Browsable = false with get, set

  member this.BuildJsonReply x : WebPart =
    let json = Newtonsoft.Json.JsonConvert.SerializeObject(x)
    Writers.setMimeType "application/json" >=> Successful.OK json

  member this.SendJsonMessage (dest: Uri) x =
    let json = Newtonsoft.Json.JsonConvert.SerializeObject(x)
    let data = System.Text.Encoding.UTF8.GetBytes(json)
    use client = new WebClient()
    client.Headers.[HttpRequestHeader.ContentType] <- "application/json"
    client.UploadData(dest, data)

  member this.SendREST dest data =
    let entryToString (KeyValue(k,v)) =
      let ek = Uri.EscapeDataString(k)
      match v with
      | None -> ek
      | Some v ->
        let ev = Uri.EscapeDataString(v)
        sprintf "%s=%s" ek ev

    let args = Seq.map entryToString data |> String.concat "&"
    use c = new WebClient()
    c.DownloadString(Uri(dest, "?" + args))

type DriverModule() =
  inherit Module()

  member this.RegisterHttpEvent(path, ?msTimeout) = this.RegisterEvent(path, httpResponse msTimeout)
  member this.RegisterGenericWebPart(wp) = this.Register(wp)
