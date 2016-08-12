namespace IoX

module Utils =
  open Newtonsoft.Json
  open System.IO

  let readConfigurationFile fn =
    JsonConvert.DeserializeObject<_>(File.ReadAllText(fn))

  let writeConfigurationFile fn conf =
    File.WriteAllText(fn, JsonConvert.SerializeObject(conf))

  type ConfigurationFile<'T>(fn: string, def: 'T) =
    let mutable conf = def

    member this.Data
      with get() = conf
      and set(v) = conf <- v

    member this.Load() =
      try conf <- readConfigurationFile(fn)
      with :? System.IO.FileNotFoundException -> ()

    member this.Save() =
      writeConfigurationFile fn conf


module Json =
  open Suave.Operators
  open Newtonsoft.Json.Linq

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
