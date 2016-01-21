namespace IoX

open EvReact.Expr
open Suave
open Suave.Files
open Suave.Successful
open Suave.EvReact
open Suave.Json
open Newtonsoft.Json.Linq

open IoX.Modules
open IoX.ModulesManager
open IoX.EmbeddedModules

module Runtime =  
  type Configuration() =
    let mutable suaveconf = defaultConfig

    member this.SuaveConfig 
      with get() = suaveconf
      and set(v) = suaveconf <- v

  let run(conf:Configuration) =
    IoX.Server.Conf.conf.Load()
    //IoX.Server.Conf.conf.Save()
    let iox = IoxModule()
    registerModule iox
    enumInstalledModules()
    preloadModules()
    let app = chooseModule()
    startWebServer conf.SuaveConfig app