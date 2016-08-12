namespace IoX.LocalAuth

open IoX.Modules
open IoX.Modules.SessionHandler
open IoX.LocalAuth
open IoX.Json
open IoX.Storage

open Suave
open Suave.Authentication
open Suave.Cookie
open Suave.EvReact
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.State
open Suave.State.CookieStateStore
open Suave.Successful

open Newtonsoft.Json
open EvReact.Expr

open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions


[<Module(
  Name = "Auth",
  Description = "IoX Auth module."
)>]
type AuthModule(data: IModuleData<unit>) as this =
  inherit IoX.Modules.DriverModule()

  let (|EmailAddress|_|) input =
      let m = Regex.Match(input, @"\A[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\z")
      if (m.Success) then Some input else None 

  let (|SignupPassword|_|) input = 
      let m = Regex.Match(input, @"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{6,15}$")
      if (m.Success) then Some input else None 

  let signupModuleEval (arg:HttpEventArgs) =
      let req = arg.Context.request
      match req.["signupName"], req.["signupSurname"], req.["signupEmail"], req.["signupUsername"], req.["signupPassword"], req.["signupRPassword"] with
      | Some name, Some surname, Some email, Some username, Some password, Some rpassword  -> 
        let checkedName = 
            match name with 
            | "" -> JProp("signupName", JVal("Error: Name can not be empty."))
            | x when (not(Regex.IsMatch(x, "([a-zA-Z]{1,30}\s*)+"))) -> JProp("signupName", JVal("Name can only contain characters"))
            | _ -> JProp("signupName", JVal("")) 
        let checkedSurname = 
            match surname with 
            | "" -> JProp("signupSurname", JVal("Error: Surname can not be empty."))
            | x when (not(Regex.IsMatch(x, "([a-zA-Z]{1,30}\s*)+"))) -> JProp("signupSurname", JVal("Surname can only contain characters"))
            | _ -> JProp("signupSurname", JVal("")) 
        let checkedEmail = 
            match email with 
            | EmailAddress x -> JProp("signupEmail", JVal("")) 
            | _ -> JProp("signupEmail", JVal("Email is not correct. Please enter a valid email address ")) 
        let checkedUsername = 
            match username with 
            | "" -> JProp("signupUsername", JVal("Error: Username cannot be empty"))
            | x when not(Regex.IsMatch(x, "^\S+$")) -> JProp("signupUsername", JVal("Username can not contain spaces"))
            | x when (isLocalUsernameRegistered username) -> JProp("signupUsername", JVal("This username is already registered"))
            | x -> JProp("signupUsername", JVal("")) 
        let checkedPassword = 
            match password with 
            | SignupPassword x -> JProp("signupPassword", JVal("")) 
            | _ -> JProp("signupPassword", JVal("Password must be 6-24 characters and must contain at least one number, one lowercase and one uppercase letter. Please enter a valid password")) 
        let checkedRPassword = 
            if rpassword.Equals(password) 
            then JProp("signupRPassword", JVal(""))
            else JProp("signupRPassword", JVal("Passwords do not match"))
        let fields = [checkedName; checkedSurname; checkedEmail; checkedUsername; checkedPassword; checkedRPassword] 
        let jsonResponse = JObj fields |> toJson
        let valid = fields |> List.forall (function JProp(_, JVal(x)) when x = box "" -> true | _ -> false)
        printfn "[signupModuleEval] %s" (jsonResponse.ToString())
        (Writers.setMimeType "application/json" >=> OK (jsonResponse.ToString())), valid
        | _ -> Suave.RequestErrors.BAD_REQUEST("Bad request"), false


  let signupModuleReq (arg:HttpEventArgs) =
      printfn "[signupModuleReq] -- Start..."
      match signupModuleEval arg with
      | res,false -> printfn "[signupModuleReq] Validation with errors"
                     arg.Result <- res
      | _,true ->
        printfn "[signupModuleReq] Validation without errors"
        let req = arg.Context.request
        arg.Result <-
        match req.["signupName"], req.["signupSurname"], req.["signupEmail"], req.["signupUsername"], req.["signupPassword"], req.["signupRPassword"] with
        | Some name, Some surname, Some email, Some username, Some password, Some rpassword  -> 
            printfn "[signupModuleReq] Storing data.."
            let pw = (encrypt password)
            let insertionRes =
                if (insertLocalUser name surname email username pw)
                then printfn "[signupModuleReq] Insert user Name:%s Surname:%s Username:%s" name surname username 
                     JProp("Errors", JVal(""))
                else JProp("Errors", JVal("Something is gone wrong!"))
            printfn "Checking errors"
            let jsonResponse = JObj [JProp("signupName", JVal("")); JProp("signupSurname", JVal("")); JProp("signupEmail", JVal("")); JProp("signupUsername", JVal("")); JProp("signupPassword", JVal("")); JProp("signupRPasswordame", JVal("")); insertionRes] 
            let containsErrors (x:Json) =
                match x with
                | JObj s -> s |> Seq.exists(function JProp(_, JVal(v)) -> v <> ("" :> obj) | _ -> false)
                | _ -> false
            if(containsErrors(jsonResponse))
            then Writers.setMimeType "application/json" >=> Suave.RequestErrors.BAD_REQUEST((jsonResponse |> toJson).ToString())
            else 
                printfn "%s" ((jsonResponse |> toJson).ToString())    
                //Writers.setMimeType "application/json" >=> OK ((jsonResponse |> toJson).ToString()) 
                if (isLocalUserRegistered username pw) then 
                    let guid = getGuidUserDataAuth username
                    let role = (getUserDataAuth guid.Value).Value.Role
                    let aut = authenticateUser (guid.Value.ToString()) (username) (role)
                    printfn "[signupModuleReq] Sending %s" ((jsonResponse |> toJson).ToString()) 
                    aut >=> Writers.setMimeType "application/json" >=> OK ((jsonResponse |> toJson).ToString()) 
                else 
                    Suave.RequestErrors.BAD_REQUEST("Bad request")  
        | _ -> Suave.RequestErrors.BAD_REQUEST("Bad request")  

  let signinModuleEval (arg:HttpEventArgs) =
      let req = arg.Context.request
      match req.["signinUsername"], req.["signinPassword"] with
      | Some username, Some password  -> 
        let checkedUsername = 
            match username with 
            | "" -> JProp("signinUsername", JVal("Error: Username cannot be empty"))
            | x when not(isLocalUsernameRegistered username) -> JProp("signinUsername", JVal("This username is not registered"))
            | x -> JProp("signinUsername", JVal("")) 
        let checkedPassword = 
            match password with 
            | "" -> JProp("signinPassword", JVal("Error: Password cannot be empty"))
            | x -> JProp("signinPassword", JVal("")) 
        let fields = [checkedUsername; checkedPassword] 
        let jsonResponse = JObj fields |> toJson
        let valid = fields |> List.forall (function JProp(_, JVal(x)) when x = box "" -> true | _ -> false)
        printfn "[signinModuleEval] %s" (jsonResponse.ToString())
        (Writers.setMimeType "application/json" >=> OK (jsonResponse.ToString())), valid
        | _ -> Suave.RequestErrors.BAD_REQUEST("Bad request"), false

//  let signinModuleReq (arg:HttpEventArgs) =
//    let username, password = 
//        match arg.Context.request.formData("signinUsername"), arg.Context.request.formData("signinPassword") with 
//        | Choice1Of2 user, Choice1Of2 pass -> sprintf "%s" user, sprintf "%s" pass //let hashPw = IoX.OAuthModules.hash(pw)
//        | _ -> "Error", "Error"
//    printfn "[signinModuleReq] Received Username:%s Password:%s" username password
//    let pw = (encrypt password)
//    if (isLocalUserRegistered username pw) then 
//        let guid = getGuidUserDataAuth username
//        let role = (getUserDataAuth guid.Value).Value.Role
//        let aut = authenticateUser (guid.Value.ToString()) (username) (role.ToString())
//        let jsonAuthResponse = JObj [JProp("username", JVal(username)); JProp("role", JVal(role.ToString()));]
//        printfn "[signinModuleReq] jsonAuthResponse:%s" ((jsonAuthResponse |> toJson).ToString()) 
//        arg.Result <- aut >=> Writers.setMimeType "application/json" >=> OK ((jsonAuthResponse |> toJson).ToString()) 
//    else 
//        arg.Result <- Suave.RequestErrors.BAD_REQUEST("Bad request")  

  
  let signinModuleReq (arg:HttpEventArgs) =
      printfn "[signinModuleReq] -- Start..."
      match signinModuleEval arg with
      | res,false -> printfn "[signinModuleReq] Validation with errors"
                     arg.Result <- res
      | _,true ->
        printfn "[signinModuleReq] Validation without errors"
        let req = arg.Context.request
        arg.Result <-
            match req.["signinUsername"], req.["signinPassword"] with
            | Some username, Some password -> 
                printfn "[signinModuleReq] Received Username:%s Password:%s" username password
                let pw = (encrypt password)
                if (isLocalUserRegistered username pw) then 
                    let guid = getGuidUserDataAuth username
                    let role = (getUserDataAuth guid.Value).Value.Role
                    let aut = authenticateUser (guid.Value.ToString()) (username) (role)
                    let jsonAuthResponse = JObj [JProp("username", JVal(username)); JProp("role", JVal(role.ToString()));]
                    printfn "[signinModuleReq] jsonAuthResponse:%s" ((jsonAuthResponse |> toJson).ToString()) 
                    aut >=> Writers.setMimeType "application/json" >=> OK ((jsonAuthResponse |> toJson).ToString()) 
                else 
                   Suave.RequestErrors.BAD_REQUEST("Bad request")  
            | _ -> Suave.RequestErrors.BAD_REQUEST("Bad request")


  let logoutModuleReq (arg:HttpEventArgs) =
      arg.Result <- reset >=> Writers.setMimeType "application/json" >=> OK "{}" //add for using the js function logout() 


  do
    this.Root <- Suave.Redirection.moved_permanently "index.html"
    this.Browsable <- true

    oauthStorage <- (loadStorage __SOURCE_DIRECTORY__).Value

    let signinModuleEvent = this.RegisterHttpEvent("signin")
    let signinModuleValidation = this.RegisterHttpEvent("signin-validation")
    let signupModuleEvent = this.RegisterHttpEvent("signup")
    let signupModuleValidation = this.RegisterHttpEvent("signup-validation")
    let logoutModuleEvent = this.RegisterHttpEvent("logout")

    +(!!signinModuleValidation |-> fun arg -> arg.Result <- signinModuleEval arg |> fst) |> this.ActivateNet |> ignore
    +(!!signinModuleEvent |-> signinModuleReq) |> this.ActivateNet |> ignore
    +(!!signupModuleValidation |-> fun arg -> arg.Result <- signupModuleEval arg |> fst) |> this.ActivateNet |> ignore
    +(!!signupModuleEvent |-> signupModuleReq) |> this.ActivateNet |> ignore
    +(!!logoutModuleEvent |-> logoutModuleReq) |> this.ActivateNet |> ignore

  static member DefaultConfig = ()
