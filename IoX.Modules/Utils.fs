module IoX.Utils

open Newtonsoft.Json
open System.IO

let readConfigurationFile<'T> fn =
  JsonConvert.DeserializeObject<'T>(File.ReadAllText(fn))

let writeConfigurationFile<'T> fn conf =
  File.WriteAllText(fn, JsonConvert.SerializeObject(conf))
