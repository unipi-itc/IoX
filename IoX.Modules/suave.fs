namespace IoX

module Suave =
  open Suave
  open System.Text.RegularExpressions

  let regex pattern : WebPart =
    let rx = Regex(pattern, RegexOptions.Compiled)
    fun ctx ->
      if rx.IsMatch(ctx.request.url.AbsolutePath) then
        succeed ctx
      else
        fail

  type WebPartDispatcher(keyId) =
    let webparts = System.Collections.Generic.Dictionary<_,_>()

    member this.Add(k, wp) = webparts.Add(k, wp)
    member val WebPart : WebPart =
      fun ctx ->
        match webparts.TryGetValue(ctx.userState.[keyId]) with
        | (false, _) -> fail
        | (true,wp) -> wp ctx

  type WebPartArray() =
    let webparts = System.Collections.Generic.List<WebPart>()

    member this.Add(wp) = webparts.Add(wp)
    member val WebPart : WebPart =
      fun ctx ->
        webparts
        |> Seq.tryPick (fun wp -> Async.RunSynchronously (wp ctx))
        |> async.Return
