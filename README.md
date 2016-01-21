## IoX runtime

The Internet of everything interconnection runtime (IoX) is a .NET based cross platform message processing system based on HTTP
and JSON to define a multivendor runtime for IoT. Vendors can develop modules quickly while the runtime ensure some degree of
isolation and filtering on the messages to enforce privacy and security.

IoX has been developed with F# and the Suave Web server library. It is very lightweight and has been validated on Linux, Windows,
and also on the Dell Networking OS 10 running on the control plane of a Dell Networking switch S6000-ON and controlling it using
the CPS interface.

We implemented a fingerprint-based door lock system with two nodes partecipating in the process.

## Status

This is the very first version of the runtime. We will soon post instructions to build and run the hello world module example.
There are several missing features that we are adding (authentication, filtering, utilities, etc.)

Join us in trying to define an open source lightweight runtime for the future of IoT.

# Build and run
If you use Visual Studio simply build the main solution and the Hello World module example.
You should get in bin/$(Configuration)/ all the files including IoX.exe which is the program.

When you start it simply browse to http://localhost:8080, access the menu and select "manage modules".
Load the hello world module and then access with your browser

http://localhost:8080/hw/helo
http://localhost:8080/hw/chat?msg=YourName
..
http://localhost:8080/hw/chat?msg=As+many+as+you+want
http://localhost:8080/hw/bye

try to access helo URL while chatting, it will be ignored.
Hope you like it and will to contribute!


