namespace IoX.Modules

open IoX

open Suave
open Suave.WebPart
open Suave.Successful
open Suave.Authentication
open Suave.RequestErrors
open Suave.Utils
open Suave.Logging
open Suave.Cookie
open Suave.Http
open Suave.Model.Binding
open Suave.State.CookieStateStore
open Suave.Operators
open Suave.Web

open Newtonsoft.Json

open System
open System.IO
open System.Collections.Generic


type UserLoggedOnSession = {
    Guid : string
    Username : string
    Role : RoleType
}

type Session = 
    | NoSession
    //| NewUser of NewUserNotLoggedOnSession
    | UserLoggedOn of UserLoggedOnSession


module SessionHandler =

    let passHash (pass: string) =
        use sha = Security.Cryptography.SHA256.Create()
        Text.Encoding.UTF8.GetBytes(pass)
        |> sha.ComputeHash
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let returnPathOrHome = 
        request (fun x -> 
            let path = 
                match (x.queryParam "returnPath") with
                | Choice1Of2 path -> path
                | _ -> "/index.html" //Path.home
            Redirection.FOUND path)

    let reset =
        printfn "Resetting cookies info..."
        unsetPair SessionAuthCookie
        >=> unsetPair StateCookie
        //>=> Redirection.FOUND "index.html" //comment for using the js function logout() 

    let withParam (key,value) path = sprintf "%s?%s=%s" path key value

    let redirectWithReturnPath redirection =
        request (fun x ->
            let path = x.url.AbsolutePath
            Redirection.FOUND (redirection |> withParam ("returnPath", path)))

    let session f =
        statefulForSession
        >=> context( fun r ->
            //match HttpContext.state r with
            let store = r |> HttpContext.state
            match  store with
            | None -> RequestErrors.BAD_REQUEST "Did not reset cookies?"
            | Some state -> 
                match state.get "guid", state.get "username", state.get "role" with
                    | Some guid, Some username, Some role -> printfn "[session f] UserLoggedOn with Guid:%s Username:%s" guid username
                                                             f (UserLoggedOn {Guid = guid; Username = username; Role = role})
                    | _ -> f NoSession)

    let sessionStore setF = context (fun x ->
        match HttpContext.state x with
        | Some state -> setF state
        | None -> never)

    let authenticateUser (guid : string) (username : string) (role : RoleType) =
        Authentication.authenticated Cookie.CookieLife.Session false
        >=> session (function | _ -> succeed)
        >=> sessionStore (fun store ->
            store.set "guid" guid
            >=> store.set "username" username
            >=> store.set "role" role)
        >=> returnPathOrHome

    let loggedOn f_success =
        Authentication.authenticate
            Cookie.CookieLife.Session
            false
            (fun () -> Choice2Of2(redirectWithReturnPath "loggedOn.html"))
            (fun _ -> Choice2Of2 reset)
            f_success

    let admin f_success =
        loggedOn (session (function
            | UserLoggedOn { Role = RoleType.Admin } -> f_success
            | UserLoggedOn _ -> FORBIDDEN "Only for admin"
            | _ -> UNAUTHORIZED "Not logged in"
        ))




//
////    let logon =
////        choose [
////            GET >>= (View.logon "" |> html)
////            POST >>= bindToForm Form.logon (fun form ->
////                let ctx = Db.getContext()
////                let (Password password) = form.Password
////                match Db.validateUser(form.Username, passHash password) ctx with
////                | Some user ->
////                    authenticateUser user
////                | _ ->
////                    View.logon "Username or password is invalid." |> html
////            )
////        ]
   