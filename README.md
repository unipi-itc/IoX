## IoX runtime

The Internet of everything interconnection runtime (IoX) is a cross platform message processing system based on HTTP
and JSON to define a multivendor runtime for IoT. Vendors can develop modules quickly while the runtime ensure some degree of
isolation and filtering on the messages to enforce privacy and security.

IoX has been developed with F# and the Suave Web server library. It is very lightweight and has been validated on Linux, Windows,
and also on the Dell Networking OS 10 running on the control plane of a Dell Networking switch S6000-ON and controlling it using
the CPS interface.

We implemented a fingerprint-based door lock system with two nodes participating in the process.

## Status

This is the very first version of the runtime. We will soon post instructions to build and run the hello world module example.
There are several missing features that we are adding (authentication, filtering, utilities, etc.)

Join us in defining an open source lightweight runtime for the future of IoT.

## Build and run

If you use Visual Studio or Xamarin Studio or any other F# tooling, simply build the main solution and the Hello World module example.
In case you are not using Visual Studio you have to manually copy the static folder in IoX.ModuleExamples.HelloWorld in the Modules 
folder of the final output.

You should get in bin/$(Configuration)/ all the files including IoX.exe which is the program.

When you start it simply browse to http://localhost:8080, access the menu and select "manage modules".
Load the hello world module and then access with your browser

    http://localhost:8080/hw/helo
    http://localhost:8080/hw/chat?msg=YourName
    ...
    http://localhost:8080/hw/chat?msg=As+many+as+you+want
    http://localhost:8080/hw/bye

Try to access helo URL while chatting, it will be ignored.

We hope you like it and will contribute!


