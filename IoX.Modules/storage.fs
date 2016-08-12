namespace IoX

//open IoX.Json
//open IoX.ModulesOAuth
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Suave
open Suave.Successful
open Suave.EvReact
open System
open System.IO
open System.Collections.Generic
open System.Linq

type RoleType = 
    | Admin
    | User
    | Guest

type AccountType = 
    | Local
    | Provider

type LocalUserData = { Name : string; Surname : string; mutable Email : string; Username : string; Password : string }
type ProviderUserData = { ID : string; Name : string; ProviderName: string; mutable AccessToken : string }
type UserData = { Account : AccountType; Role : RoleType; Local : LocalUserData; Provider: ProviderUserData } //; mutable LoggedIn : bool }


module Storage =

    let defaultLocalUserData = { Name = ""; Surname = ""; Email = ""; Username = ""; Password = "" }
    let defaultProviderUserData = { ID = ""; Name = ""; ProviderName = ""; AccessToken = "" }
    
    let mutable oauthStorage = Dictionary<Guid, UserData>()
    
    let encrypt(pw : string) = 
        use sha = System.Security.Cryptography.SHA256.Create()
        System.Text.Encoding.UTF8.GetBytes(pw)
        |> sha.ComputeHash
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let loadStorage dir =
        let storageFile = Path.Combine(dir, "storage.json")
        if File.Exists(storageFile) then
            printfn "File exists"
            let m = JsonConvert.DeserializeObject<Dictionary<Guid, UserData>>(File.ReadAllText(storageFile))
            if not(m.Count = 0) then
                Some m
            else
                None
        else
            None

    let saveStorage dir =
        let storageFile = Path.Combine(dir, "storage.json")
        File.WriteAllText(storageFile, JsonConvert.SerializeObject(oauthStorage, Formatting.Indented)) 
    
    let isLocalUsernameRegistered username =
        (oauthStorage.Where(fun x -> x.Value.Local.Username = username).Count()) >= 1

    let isLocalUserRegistered username passwordHash = 
        (oauthStorage.Where(fun x -> x.Value.Local.Username = username && x.Value.Local.Password = passwordHash).Count()) >= 1
    
    let getUserDataAuth guid = 
        if oauthStorage.ContainsKey(guid) then Some oauthStorage.[guid]
        else None
    
    let getGuidUserDataAuth username =
        oauthStorage |> Seq.tryFind (fun x -> x.Value.Local.Username = username) |> Option.map (fun elem -> elem.Key)
    
    let getUserAuths() = oauthStorage.Values |> Seq.map (fun p -> p)        

    let insertLocalUser (name : string) (surname : string) (email : string) (username : string) (passwordHash : string) : bool = 
        if (isLocalUsernameRegistered username) then false
        else 
            let newLocalUser = { Name = name; Surname = surname; Email = email; Username = username; Password = passwordHash }
            let newRecord = { Account = Local; Role = RoleType.User; Local = newLocalUser; Provider = defaultProviderUserData } //; LoggedIn = false }
            try
                oauthStorage.Add(Guid.NewGuid(), newRecord)
                true
            with
                | :? System.ArgumentNullException -> false
                | :? System.ArgumentException -> false
            
    let isProviderUserRegistered id =
        (oauthStorage.Where(fun x -> x.Value.Provider.ID = id).Count()) >= 1

    let insertProviderUser (providerName : string) (id : string) (name : string) (accessToken : string) = 
        if (isProviderUserRegistered id) then false
        else 
            let newProviderUser = { ID = id; Name = name; ProviderName = providerName; AccessToken = accessToken }
            let newRecord = { Account = Provider; Role = RoleType.User; Local = defaultLocalUserData; Provider = newProviderUser } //; LoggedIn = false }
            try
                oauthStorage.Add(Guid.NewGuid(), newRecord)
                true
            with
                | :? System.ArgumentNullException -> false
                | :? System.ArgumentException -> false

    let readAll() = 
        if oauthStorage.Count = 0 then printfn "UserStorage is empty"
        oauthStorage |> Seq.iter (fun (KeyValue(k, v)) -> 
                           printfn "Key: %s" (k.ToString())
                           printfn "Value: %A\r" v)
    
    let printUsers() = 
        if oauthStorage.Count = 0 then printfn "OAuthStorage is empty"
        let sb = new System.Text.StringBuilder()
        oauthStorage |> Seq.iter (fun (KeyValue(k, v)) -> 
                           sb.AppendFormat
                               ("Account: {0} \tRole: {1} \tName: {1} \tSurname: {2} \t\Email: {3} \t\ID: {4} \tName: {5} \tProviderName: {6} \tAccessToken: {7}", 
                                v.Account, v.Role, v.Local.Name, v.Local.Surname, v.Local.Email, v.Provider.ID, v.Provider.Name, v.Provider.ProviderName, v.Provider.AccessToken) 
                           |> ignore
                           sb.AppendLine() |> ignore)
        printfn "Users"
        printfn "%s" (sb.ToString())
        sb.ToString()
