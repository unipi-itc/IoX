#I __SOURCE_DIRECTORY__
#r @"packages\Suave.1.0.0\lib\net40\Suave.dll"
#r @"packages\Newtonsoft.Json.8.0.2\lib\net40\Newtonsoft.Json.dll"

Newtonsoft.Json.Linq.JToken.Parse(";-[[")


#load "suave.fs"

open Suave
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful

open IoX.Suave

let contentType (t:string) (arg:HttpContext) =
    async {
      match arg.request.header("content-type") with
      | Choice1Of2 v ->
        if v = t then
          return Some arg
        else
          return None
      | Choice2Of2 v ->
        return None
    }

let ok () =
  fun arg -> printfn "%s" (arg.request.url.ToString()); OK "ok" arg

let app = choose [ contentType "application/json" >=> ok(); OK "Bad" ]

startWebServer defaultConfig app

let json = @"
{ ""foo"": 2,
  ""baz"": [ 2, 3, 4 ],
  ""bar"": ""value""
}
"

open Newtonsoft.Json.Linq

let v = JObject.Parse(json)



v.["foo"].Value<int>()
v.["baz"].[0].Value<int>()


v.["foo"] <- JToken.FromObject("Ciao")
v.ToString()

let d = System.Collections.Generic.Dictionary<string,obj>()
d.["foo"] <- 2
d.["baz"] <- [| 1.1; 2.0 |]



JObject.FromObject(d).ToString()


v.["foo"].Type

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


let d1 = JObj [ JProp("foo", !? 2); JProp("baz", JArr [ !? 1.1; !? 2.0 ]) ]
(toJson d1).ToString()


