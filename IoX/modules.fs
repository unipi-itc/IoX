namespace IoX

open Suave
open Suave.Operators
open Suave.Files
open IoX.Suave
open Suave.EvReact
open EvReact
open IoX.Modules

open System.Collections.Generic
open System.IO

module ModulesManager = 
  let availableModules = Dictionary<string, Conf.ModuleDescriptor>()
  let loadedModules = ResizeArray<Module>()
  let loadedModulesIdx = Dictionary<string, Module>()

  let findModule name = loadedModules |> Seq.tryFind (fun m -> m.Name = name)

  let registerModule (m:Module) =
    // Checks needed here
    loadedModules.Add(m)
    loadedModulesIdx.Add(m.Name, m)
    m.OnLoad()

  let loadModuleConf dir =
    let sep = Path.DirectorySeparatorChar
    let conf = sprintf "%s%ciox.conf" dir sep
    if File.Exists(conf) then
      try
        let m = Newtonsoft.Json.JsonConvert.DeserializeObject<Conf.ModuleDescriptor>(File.ReadAllText(conf))
        if DirectoryInfo(dir).Name = m.Name then
          Some m
        else
          None
      with _ -> None
    else
      None

  let enumInstalledModules () =
    let sep = Path.DirectorySeparatorChar
    Directory.GetDirectories(Module.ModulesPath) 
      |> Seq.filter (fun dn -> 
        printfn "%s" (sprintf "%s%ciox.conf" dn sep)
        File.Exists(sprintf "%s%ciox.conf" dn sep))
      |> Seq.iter (fun d -> 
                     match loadModuleConf(d) with
                     | Some m ->
                       availableModules.Add(m.Name, m)
                     | None -> ()
                  )

  let loadModule (name:string) =
    let sep = Path.DirectorySeparatorChar
    let dir = sprintf "%s%c%s" Module.ModulesPath sep name
    if not(Directory.Exists(dir)) then
      failwith (sprintf "Error loading module %s: missing directory '%s'" name dir)
    let conf = sprintf "%s%ciox.conf" dir sep
    if not(File.Exists(conf)) then
      failwith (sprintf "Error loading module %s: missing configuration file '%s'" name conf)
    let c = Newtonsoft.Json.JsonConvert.DeserializeObject<Conf.ModuleDescriptor>(File.ReadAllText(conf))
    let assemblyfile = sprintf "%s%c%s" dir sep c.Assembly
    if (not(File.Exists(assemblyfile))) then
      failwith (sprintf "Error loading module %s: missing assembly file '%s'" name assemblyfile)
    let assembly = System.Reflection.Assembly.LoadFile(assemblyfile)
    match (assembly.GetTypes() |> Array.tryFind(fun t -> t.IsSubclassOf(typeof<Module>))) with
    | Some t ->
      let m = assembly.CreateInstance(t.FullName) :?> Module
      registerModule(m)
    | None ->
      failwith (sprintf "Error loading module %s: no type is inheriting from IoX.Modules.Module in '%s'" name assemblyfile)

  let preloadModules () =
    let c = IoX.Server.Conf.conf.Configuration
    for mn in c.OnLoadModules do
      loadModule(mn)

  let chooseModule () =
      let rec intchoose (idx) =
        fun arg -> async {
          if idx = loadedModules.Count then 
            return! Suave.RequestErrors.NOT_FOUND (sprintf "Requested URL %s not found." arg.request.url.PathAndQuery) arg
          else
            let m = loadedModules.[idx]
            let attemptStatic () =
              async {
                if m.Browsable then
                  let sep = System.IO.Path.DirectorySeparatorChar
                  let! res = browse m.ModuleStaticFilesPath arg
                  match res with
                  | Some x -> return Some x
                  | None   ->
                  return None
                else
                  return None
              }

            if m.Name = Module.ROOT || arg.request.url.LocalPath.StartsWith(sprintf "/%s/" m.Name) then
              let wp = m.Verbs.choose()
              let! res = wp arg

              match res with
              | Some x  -> return Some x
              | None ->
                let! statres = attemptStatic()
                match statres with
                | Some x -> return Some x
                | None -> 
                  if idx = 0 then
                    return! intchoose (idx + 1) arg
                  else
                    let! f = RequestErrors.FORBIDDEN "" arg
                    return f

            else
              return! intchoose (idx + 1) arg
        }
      intchoose 0
