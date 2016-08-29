namespace IoX.ModuleExamples.HelloWorld

open IoX.Modules
open EvReact.Expr
open Suave.EvReact
open Suave.Successful

[<Module(
  Name = "Hello world",
  Description = "Example IoX module."
)>]
type HelloWorldModule(data: IModuleData<unit>) as this =
  inherit Module()

  do
    this.Root <- Suave.Redirection.moved_permanently "index.html"
    this.Browsable <- true

    let hello = this.RegisterReplyEvent("helo")
    let chat = this.RegisterReplyEvent("chat")
    let bye = this.RegisterReplyEvent("bye")

    +(
      (!!hello |-> fun (arg:MsgRequestEventArgs<_>) -> arg.Result <- OK "Hello dear")
      -
      +(!!chat |-> fun (arg:MsgRequestEventArgs<_>) -> arg.Result <- OK (sprintf "I disagree on %s" arg.Message) ) / [| bye |]
      -
      (!!bye |-> fun (arg:MsgRequestEventArgs<_>) -> arg.Result <- OK "Bye bye!")
    )
    |> this.ActivateNet
    |> ignore

  static member DefaultConfig = ()
