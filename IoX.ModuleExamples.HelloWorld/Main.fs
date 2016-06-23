namespace IoX.ModuleExamples.HelloWorld

open IoX.Modules
open EvReact.Expr
open Suave
open Suave.Files
open Suave.Filters
open Suave.Successful
open Suave.EvReact
open Newtonsoft.Json.Linq
open IoX.Json

type HelloWorldModule() as this =
  inherit Module("hw", "Hello world", "Example module")

  do this.Browsable <- true

  override this.OnLoad() =
    let hello = this.RegisterEvent("/hw/helo")
    let chat = this.RegisterEvent("/hw/chat")
    let bye = this.RegisterEvent("/hw/bye")

    let send uri msg = this.CreateRemoteTrigger uri msg

    let net = 
      +(
        (!!hello |-> fun (uri, _) -> send uri "Hello dear")
        -
        +(!!chat |-> fun (uri, msg) -> send uri (sprintf "I disagree on %s" msg) ) / [|bye|]
        -
        (!!bye |-> fun (uri, _) -> send uri "Bye bye!")
        )
      
    this.ActivateNet(net) |> ignore
