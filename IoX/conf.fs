module IoX.Server.Conf
  type IoXConfiguration = { OnLoadModules : string array }
  let defaultIoXConfiguration = { OnLoadModules = [||] }

  let mutable conf = new IoX.Modules.Conf.ConfigurationFile<_>("iox.conf", defaultIoXConfiguration)

