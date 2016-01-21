namespace IoX

open Suave
open Suave.Filters
open Suave.WebPart
open Suave.Operators
open Newtonsoft.Json.Linq
open System.Text.RegularExpressions

module Json =
  let parseJson (s:string) =
    JToken.Parse(s)

  type Json =
  | JObj of Json seq
  | JProp of string * Json
  | JArr of Json seq
  | JVal of obj

  let (!?) (o : obj) = JVal o

  let rec toJson = function
  | JVal v -> new JValue(v) :> JToken
  | JProp(name, (JProp(_) as v)) -> new JProperty(name, new JObject(toJson v)) :> JToken
  | JProp(name, v) -> new JProperty(name, toJson v) :> JToken
  | JArr items -> new JArray(items |> Seq.map toJson) :> JToken
  | JObj props -> new JObject(props |> Seq.map toJson) :> JToken


module Suave =
  let regexResponseContentFilter (regs:Regex array) =
    fun (arg:HttpContext) ->
      async {
        let txt = arg.response.content.ToString()
        match (regs |> Array.tryFind(fun re -> re.IsMatch(txt))) with
        | Some _ -> return None
        | None -> return Some arg
      }

  type WebPartArray<'a>() =
    let webparts = ResizeArray<WebPart<'a>>()
    let mutable filter : WebPart<_> option = None

    member this.Filter
      with get() = filter
      and set(v) = filter <- v

    member this.Add(wp:WebPart<'a>) = webparts.Add(wp)
    member this.Remove(wp:WebPart<'a>) = webparts.Remove(wp)
    member this.Clear () = webparts.Clear()

    member this.GetEnumerator() = webparts.GetEnumerator()

    member this.Count with get() = webparts.Count

    member this.Item with get(idx) = webparts.[idx]

    member this.choose () =
      let rec intchoose (idx) =
        fun arg -> async {
          if idx = webparts.Count then return None
          else
            let wp = webparts.[idx]
            let! res = wp arg
            match res with
            | Some x -> return Some x
            | None -> return! intchoose (idx + 1) arg
        }
      match filter with
      | Some wp -> wp >=> (intchoose 0)
      | None    -> intchoose 0
