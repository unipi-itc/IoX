namespace IoX.OAuth

open IoX.Modules
open IoX.Modules.SessionHandler
open IoX.Storage
open IoX.Json

open Suave
open Suave.Authentication
open Suave.Cookie
open Suave.EvReact
open Suave.Filters
open Suave.OAuth
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


type OAuthDescriptor = {
    authorize_uri: string;
    exchange_token_uri: string;
    client_id: string;
    client_secret: string;
    request_info_uri: string;
    scopes: string;
    token_response_type: DataEnc;
    //customize_req: HttpWebRequest -> unit;
}


[<Module(
  Name = "OAuth",
  Description = "IoX OAuth module."
)>]
type OAuthModule(data: IModuleData<unit>) as this =
  inherit IoX.Modules.DriverModule()

  let mutable oauthMap = Dictionary<string, OAuthDescriptor>()
  let mutable authWebPart = Unchecked.defaultof<_> // TO CHECK

  let addProvider providerName authUri tokenUri clientID clientSecret reqInfoUri scps (tokenRespType:DataEnc) = 
    let providerData = {authorize_uri = authUri; exchange_token_uri = tokenUri; client_id = clientID; client_secret = clientSecret; request_info_uri = reqInfoUri; scopes = scps; token_response_type = tokenRespType}
    oauthMap.Add(providerName, providerData)

  let delProvider providerName = 
    oauthMap.Remove(providerName)

  let saveOAuthConf dir = 
    let sep = Path.DirectorySeparatorChar
    let suaveConf = sprintf @"%s%coauth.conf" dir sep
    File.WriteAllText( suaveConf, JsonConvert.SerializeObject(oauthMap, Formatting.Indented) )

  let loadOAuthConf dir =
    let sep = Path.DirectorySeparatorChar
    let suaveConf = sprintf @"%s%coauth.conf" dir sep
    if File.Exists(suaveConf) then
        printfn "File exists"
        let m = JsonConvert.DeserializeObject<Dictionary<string,OAuthDescriptor>>(File.ReadAllText(suaveConf))
        if not(m.Count = 0) then
            Some m
        else
            None
    else
        None

  let preloadProviders =
    let sep = Path.DirectorySeparatorChar
    let dir = __SOURCE_DIRECTORY__ //let dir = System.Environment.CurrentDirectory
    if not(Directory.Exists(dir)) then
        failwith (sprintf "Error loading oauth.conf: missing directory '%s'" dir)
    match loadOAuthConf dir with
    | Some x -> oauthMap <- x
    | _ -> ()

  let oauthConfigs = 
    //let oauthdescriptor2providerconfig (pname) (x:OAuthDescriptor) =
    let oauthdescriptor2providerconfig (x:OAuthDescriptor) =
        {
        OAuth.EmptyConfig with
            authorize_uri = x.authorize_uri
            exchange_token_uri = x.exchange_token_uri
            request_info_uri = x.request_info_uri
            scopes = x.scopes
            client_id = x.client_id
            client_secret = x.client_secret
            token_response_type = x.token_response_type;
        }
//        oauthMap
//        |> Seq.map (fun x -> 
//                            printfn "name:%s value:%A" x.Key x.Value
//                            x.Key, x.Value)
//        |> Map.ofSeq
//        |> Map.map oauthdescriptor2providerconfig
    seq {
        for e in oauthMap do
            let pname = e.Key
            let pdef = oauthdescriptor2providerconfig e.Value
            yield pname, pdef
    }
    |> Map.ofSeq

  let loadProvidersReq (arg:HttpEventArgs) =
    let v = JsonConvert.SerializeObject(oauthMap, Formatting.Indented);
    printfn "[loadProvidersReq] Serializing oauthMap: %s" v
    arg.Result <- Writers.setMimeType "application/json" >=> OK (v.ToString())

  let oauthBuildLoginUrl (ctx:HttpContext) =
        let bb = new System.UriBuilder (ctx.request.url)
        bb.Host <- ctx.request.host
        let loginpath = ctx.request.url.Segments
        loginpath.SetValue("oalogin", loginpath.Length-1)
        bb.Path <- loginpath |> String.concat "" //"oalogin"
        bb.Query <- ""
        bb.ToString()

  let authModuleOAQueryReq (arg:HttpEventArgs) =
    arg.Result <- GET >=> context(fun ctx -> redirectAuthQuery oauthConfigs (oauthBuildLoginUrl ctx)) 

  let authModuleOALoginReq (arg:HttpEventArgs) =
    let fnLogin = 
        (fun loginData ctx -> 
            let res = insertProviderUser loginData.ProviderName loginData.Id loginData.Name loginData.AccessToken
            if res then 
                ctx |> Redirection.FOUND "loggedOnOAuth.html"
            else 
                ctx |> Redirection.redirect "loggedOnOAuth.html" //TODO redirect to the error page
            )           
    let fnFailure = 
        (fun error -> 
            printfn "fun error"
            OK <| sprintf "Authorization failed because of `%s`" error.Message)
    arg.Result <- 
        GET >=>
            context(fun ctx ->
                processLogin oauthConfigs (oauthBuildLoginUrl ctx)
                    (fnLogin >> (fun wpb -> Authentication.authenticated Cookie.CookieLife.Session false >=> wpb))
                    fnFailure
            )

  let authModuleLogoutReq (arg:HttpEventArgs) =
    let fnLogout = 
        (fun () -> Redirection.FOUND "loggedOutOAuth.html") 
    arg.Result <- 
        GET >=> 
            context(fun ctx ->
                let cont = fnLogout()
                unsetPair Authentication.SessionAuthCookie >=> unsetPair Suave.State.CookieStateStore.StateCookie >=> cont
            )

  do
    this.Root <- Suave.Redirection.moved_permanently "index.html"
    this.Browsable <- true

    oauthStorage <- (loadStorage __SOURCE_DIRECTORY__).Value

    let loadProviders = this.RegisterHttpEvent("load-providers")
    let authBeginEvent = this.RegisterHttpEvent("oaquery")
    let authCallbackEvent = this.RegisterHttpEvent("oalogin")
    let authLogoutEvent = this.RegisterHttpEvent("logout")

    +(!!loadProviders |-> loadProvidersReq) |> this.ActivateNet |> ignore
    +(!!authBeginEvent |-> authModuleOAQueryReq) |> this.ActivateNet |> ignore
    +(!!authCallbackEvent |-> authModuleOALoginReq) |> this.ActivateNet |> ignore
    +(!!authLogoutEvent |-> authModuleLogoutReq) |> this.ActivateNet |> ignore

  static member DefaultConfig = ()


