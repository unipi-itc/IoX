namespace IoX

module Main =

  open Suave

  [<EntryPoint>]
  let main argv =
    let path = System.IO.FileInfo(typeof<IoX.Modules.Module>.Assembly.Location).Directory.FullName
    System.Environment.CurrentDirectory <- path
    let conf = Runtime.Configuration()
    conf.SuaveConfig <-
        { defaultConfig with
            bindings = 
              [ HttpBinding.mkSimple HTTP "0.0.0.0" 8080 ]
        }
    Runtime.run(conf)
    0

