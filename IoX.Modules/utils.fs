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
