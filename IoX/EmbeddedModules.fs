namespace IoX.EmbeddedModules

open IoX.Modules
open IoX.ModulesManager
open EvReact.Expr
open Suave
open Suave.Files
open Suave.Filters
open Suave.Successful
open Suave.RequestErrors
open Suave.EvReact
open Newtonsoft.Json.Linq
open IoX.Json

  type IoxModule() as this =
    inherit Module(Module.ROOT, "IoX Runtime module", "Core processing module")

    do this.Browsable <- true

    let defaultIndex (arg:HttpEventArgs) =
      arg.Result <- browseFile this.ModuleStaticFilesPath "index.html"

    let loadedModulesListReq (arg:HttpEventArgs) =
        let modules = 
          loadedModules 
          |> Seq.skip 1
          |> Seq.filter (fun m -> m.Browsable)
          |> Seq.map (fun m -> JObj [ JProp("Title", !? m.Title); JProp("Url", !?(sprintf "/%s/index.html" m.Name)) ] |> toJson)
          |> Seq.toArray
        arg.Result <- OK (JArray(modules).ToString())

    let availableModulesListReq (arg:HttpEventArgs) =
      let v = 
        availableModules
        |> Seq.map (fun kv ->
            JObj [ 
              JProp("Title", !? kv.Value.Title); 
              JProp ("Name", !? kv.Value.Name);
              JProp ("Description", !? kv.Value.Description);
              JProp ("Loaded", !? loadedModulesIdx.ContainsKey(kv.Value.Name))
            ] |> toJson
          )
        |> Seq.toArray
        |> JArray
      arg.Result <- OK (v.ToString())

    let loadModuleReq (arg:HttpEventArgs) =
      let n = arg.Context.request.query
              |> Seq.tryFind (function (k, Some v) -> k = "name" | (k, None) -> false)
      match n with
      | Some ("name", Some v) ->
        if availableModules.ContainsKey(v) && not(loadedModulesIdx.ContainsKey(v)) then
          try
            loadModule(v)
            arg.Result <- OK "Loaded"
          with _ ->
            arg.Result <- ServerErrors.INTERNAL_ERROR "Error loading module"
        else
          arg.Result <- NOT_FOUND "Module unavailable"
      | _ -> arg.Result <- NOT_FOUND "Module unavailable"

    override this.OnLoad() =
      let root = this.RegisterHttpEvent("^/$")
      let modulesList = this.RegisterHttpEvent("^/status/modules")
      let availableModulesList = this.RegisterHttpEvent("^/status/available-modules")
      let loadModuleCommand = this.RegisterHttpEvent("^/iox/load-module", filter=GET)
      +(!!root |-> defaultIndex) |> this.ActivateNet |> ignore
      +(
            (!!modulesList |-> loadedModulesListReq)
        |||
            (!!availableModulesList |-> availableModulesListReq)
      ) |> this.ActivateNet |> ignore
      +(!!loadModuleCommand |-> loadModuleReq) |> this.ActivateNet |> ignore
