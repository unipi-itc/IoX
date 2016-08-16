## IoX runtime

The Internet of everything interconnection runtime (IoX) is a cross platform message processing system based on HTTP
and JSON to define a multivendor runtime for IoT. Vendors can develop modules quickly while the runtime ensure some degree of
isolation and filtering on the messages to enforce privacy and security.

IoX has been developed with F# and the Suave Web server library. It is very lightweight and has been validated on Linux, Windows,
and also on the Dell Networking OS 10 running on the control plane of a Dell Networking switch S6000-ON and controlling it using
the CPS interface.

We implemented a fingerprint-based door lock system with two nodes participating in the process.

## Status

The project is quickly maturing, now the module system is almost finished and we are working on the security
(authentication and authorization) part. Now we use React to help defining module pages without implying extra
overhead for the IoT device (composition is performed on the client browser).

Join us in defining an open source lightweight runtime for the future of IoT.

## Build and run

To build IoX you have to build the server, the runtime (`IoX.Modules`) and the two modules part of the distribution
(`IoX.Module.Subtree` and `IoX.ModuleExamples.HelloWorld`). In order to build you have to run:

    nuget restore
    msbuild IoX.sln

You can use *xbuild* on Mono.

Note that you can also build from within Visual Studio but we are using wildcards inside `.fsproj` files that are 
not supported by the F# project system yet. In this case you should copy the static folder of the *SubTree* and
*HelloWorld* modules in the output directory.

Once everything is built you can copy the output of the *SubTree* project in a folder named `root`, and then in the
`modules/hw` folder the output of the *HelloWorld* project.

You can then run `IoX.Server.exe` and point your browser to http://localhost:8080/   

We hope you like it and will contribute!

### Build Status
|         |Linux|Windows|
|--------:|:---:|:-----:|
|**Status**|[![Build Status](https://travis-ci.org/unipi-itc/IoX.svg?branch=master)](https://travis-ci.org/unipi-itc/IoX)|[![Build status](https://ci.appveyor.com/api/projects/status/b33my40yqrru87ma?branch=master&svg=true)](https://ci.appveyor.com/project/ranma42/iox)|