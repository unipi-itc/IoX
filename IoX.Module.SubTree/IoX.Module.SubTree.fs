namespace IoX.Module.SubTree

open IoX.Modules
open IoX.Utils
open EvReact.Expr
open Suave
open Suave.EvReact
open Suave.Operators
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions

type ModuleDescriptor = {
  AssemblyPath: string
  ModuleTypeName: string
}

type ModuleInfo = {
  Id: string
  Url: string
  Name: string
  Description: string
  Loaded: bool
  Browsable: bool
}

type IModuleWrapper =
  abstract member Name : string
  abstract member Description : string
  abstract member Browsable : bool
  abstract member WebPart : WebPart

type ModuleData<'C>(id, path, url, defaultCfg) =
  interface IModuleData<'C> with
    member this.Id = id
    member this.Path = path
    member this.BaseUri = url
    member val Configuration = ConfigurationFile(Path.Combine(path, "module.conf"), defaultCfg)
    // TODO: member this.Logger = logger

type ModuleWrapper(id, path, url: Uri) =
  let loadModuleType path =
    let desc = readConfigurationFile(Path.Combine(path, "module.iox.conf"))
    let path = Path.Combine(path, desc.AssemblyPath)
    let typeName = desc.ModuleTypeName
    let assembly = Assembly.LoadFile(path)
    match assembly.GetType(typeName) with
    | null -> failwith (sprintf "Error loading %s from %s: type not found" typeName assembly.FullName)
    | m when m.IsSubclassOf(typeof<IoX.Modules.Module>) -> m
    | _ -> failwith (sprintf "Error loading %s from %s: it does not inherit from IoX.Modules.Module" typeName assembly.FullName)

  let mType = loadModuleType path
  let attr = mType.GetCustomAttributes(typeof<ModuleAttribute>) |> Seq.exactlyOne :?> ModuleAttribute

  let defaultCfg = mType.GetProperty("DefaultConfig").GetValue(null)
  let dataType =
    if isNull defaultCfg then
      typeof<ModuleData<unit>>
    else
      typedefof<ModuleData<_>>.MakeGenericType(defaultCfg.GetType())
  let data = Activator.CreateInstance(dataType, id, path, url, defaultCfg)
  let m = Activator.CreateInstance(mType, data) :?> IoX.Modules.Module

  let browse ctx =
    match ctx.userState.["relPath"] with
    | :? string as s when m.Browsable ->
      let fileName = sprintf "static/%s" s
      Files.browseFile path fileName ctx
    | _ -> fail

  interface IModuleWrapper with
    member val WebPart = m.WebPart <|> browse <|> RequestErrors.NOT_FOUND ""
    member val Name = attr.Name
    member val Description = attr.Description
    member this.Browsable = m.Browsable

type ModuleAnalyzer(id, path, url: Uri) =
  let desc = readConfigurationFile(Path.Combine(path, "module.iox.conf"))
  let assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(Path.Combine(path, desc.AssemblyPath))
  let mType =
    assembly.Modules
    |> Seq.map(fun m -> m.GetType(desc.ModuleTypeName))
    |> Seq.filter (isNull >> not)
    |> Seq.exactlyOne

  let mAttr =
    mType.CustomAttributes
    |> Seq.filter (fun x -> x.AttributeType.FullName = "IoX.Modules.ModuleAttribute")
    |> Seq.exactlyOne

  let namedArgs = Seq.append mAttr.Fields mAttr.Properties
  let extractNamedArg name =
    let namedArg = namedArgs |> Seq.filter (fun x -> x.Name = name) |> Seq.exactlyOne
    namedArg.Argument.Value :?> _

  interface IModuleWrapper with
    member val WebPart = RequestErrors.NOT_FOUND (sprintf "Module %s is not loaded" id)
    member val Name = extractNamedArg "Name"
    member val Description = extractNamedArg "Description"
    member this.Browsable = false

type ModuleLoader(id, path, url) =
  let mutable loaded = false
  let mutable wrapper = ModuleAnalyzer(id, path, url) :> IModuleWrapper

  member this.Load() =
    if not loaded then
      wrapper <- ModuleWrapper(id, path, url)
      loaded <- true

  member this.WebPart = wrapper.WebPart
  member this.Info = {
    Id          = id
    Url         = url.AbsolutePath
    Name        = wrapper.Name
    Description = wrapper.Description
    Loaded      = loaded
    Browsable   = wrapper.Browsable
  }

type IoxModuleCfg = {
  ModulesPath: string
  OnLoadModules: string[]
}

[<Module(
  Name = "IoX Runtime module",
  Description = "Core processing module"
)>]
type SubTreeModule(data: IModuleData<IoxModuleCfg>) as this =
  inherit IoX.Modules.DriverModule()
  let submodulesPath = "modules/"
  let submodulesBaseUri = Uri(data.BaseUri, submodulesPath)
  let submodulesRegex = Regex("^modules/([^/]*)/(.*)$", RegexOptions.Compiled)

  let modules = Dictionary<_,_>()

  let refreshAvailableModules _ =
    let pluginModule path _ =
      try
        let d = DirectoryInfo(path)
        let id = d.Name
        let path = d.FullName
        if modules.ContainsKey(id) then
          failwith "Module already loaded"
        else
          modules.Add(id, ModuleLoader(id, path, Uri(submodulesBaseUri, id + "/")))
      with e ->
        printfn "%A" e

    let clearIfNotLoaded _ =
      let notLoaded =
        modules.Values
        |> Seq.filter (fun m -> not m.Info.Loaded)
        |> Seq.toArray
      for m in notLoaded do modules.Remove(m.Info.Id) |> ignore

    lock modules clearIfNotLoaded
    let modulesPath = Path.Combine(data.Path, data.Configuration.Data.ModulesPath)
    for d in Directory.EnumerateDirectories(modulesPath) do
      lock modules (pluginModule d)

  let getLoadedModules (arg:MsgRequestEventArgs<_>) =
    let loadedModulesInfo _ =
      modules.Values
      |> Seq.map (fun m -> m.Info)
      |> Seq.filter (fun info -> info.Loaded)
      |> Seq.toArray

    arg.Result <-
      lock modules loadedModulesInfo
      |> Newtonsoft.Json.JsonConvert.SerializeObject
      |> Successful.OK

  let getAvailableModules (arg:MsgRequestEventArgs<_>) =
    let availableModulesInfo _ =
      modules.Values
      |> Seq.map (fun m -> m.Info)
      |> Seq.toArray

    arg.Result <-
      lock modules availableModulesInfo
      |> Newtonsoft.Json.JsonConvert.SerializeObject
      |> Successful.OK

  let loadModule (arg:MsgRequestEventArgs<string>) =
    let tryLoadModule _ =
      try
        let loader = modules.[arg.Message]
        loader.Load()
        loader.Info
        |> Newtonsoft.Json.JsonConvert.SerializeObject
        |> Successful.OK
      with e ->
        modules.Remove(arg.Message) |> ignore
        e.ToString() |> ServerErrors.INTERNAL_ERROR
    arg.Result <- lock modules tryLoadModule

  let tryGetModule id _ =
    let mutable m = Unchecked.defaultof<_>
    if modules.TryGetValue(id, &m) then
      m.WebPart
    else
      RequestErrors.NOT_FOUND (sprintf "Module %s is not available" id)

  let subModulesWP (ctx:HttpContext) =
    let m = submodulesRegex.Match(ctx.userState.["relPath"] :?> _)
    if m.Success then
      let moduleId = m.Groups.[1].Value
      let relPath = m.Groups.[2].Value
      let wp = lock modules (tryGetModule moduleId)
      wp { ctx with userState = ctx.userState.Add("relPath", relPath) }
    else
      fail

  do
    this.Root <- Redirection.moved_permanently "index.html"

    this.Browsable <- true
    this.RegisterGenericWebPart(subModulesWP)

    refreshAvailableModules ()
    for m in data.Configuration.Data.OnLoadModules do
      modules.[m].Load()

    let onGetLoadedModules = this.RegisterReplyEvent("loaded-modules")
    let onGetAvailableModules = this.RegisterReplyEvent("available-modules")
    let onRefreshAvailableModules = this.RegisterEvent("refresh-available-modules")
    let onLoadModule = this.RegisterReplyEvent("load-module")

    +(!!onGetLoadedModules |-> getLoadedModules) |> this.ActivateNet |> ignore
    +(!!onGetAvailableModules |-> getAvailableModules) |> this.ActivateNet |> ignore
    +(!!onRefreshAvailableModules |-> refreshAvailableModules) |> this.ActivateNet |> ignore
    +(!!onLoadModule |-> loadModule) |> this.ActivateNet |> ignore

  static member DefaultConfig = { ModulesPath = "modules/"; OnLoadModules = [| |] }
