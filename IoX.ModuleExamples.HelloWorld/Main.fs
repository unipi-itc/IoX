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

    let net = 
      +(
        (!!hello |-> fun arg -> arg.Result <- OK "Hello dear")
        -
        +(!!chat |-> fun arg -> 
          let msg = match arg.Context.request.query.[0] with "msg", Some m -> m | _ -> ""
          arg.Result <- OK (sprintf "I disagree on %s" msg)
        ) / [|bye|]
        -
        (!!bye |-> fun arg -> arg.Result <- OK "Bye bye!")
        )
      
    this.ActivateNet(net) |> ignore
