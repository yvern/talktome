open System.IO
open System.Text.RegularExpressions
open FSharp.Json
open FSharp.Data
open FSharp.Data.HttpRequestHeaders

type Group = { team_id: string option; channel_id: string option; name: string; display_name: string; [<JsonField("type")>] kind: string option }
type Login = { login_id: string; password: string }
type Auth = string * string
type Post = { id: string; update_at: uint64; message: string; user_id: string }
type Posts = { order: string []; posts: Map<string, Post> }
type BotState = { name: string; last: string option; auth: Auth option; login: Login; group: Group; hook: string }
let newBot login' group' hook' name' = { name= name'; last= None; auth= None; login= login'; group= group'; hook= hook' }

let mmBase path' = sprintf "http://localhost:8065%s" path'

let mmApi path' = path' |> sprintf "/api/v4%s" |> mmBase

let getReq path' header' = Http.Request(path', httpMethod = "GET", headers = header')

let sendReq method' path' header' body' = 
  Http.Request(path', httpMethod = method', headers = header', body = TextRequest body')

let userInfo (login' : Login) =
 login' |> Json.serialize |> sendReq "POST" (mmApi "/users/login") []

let getAuth (login' : Login) = 
  (userInfo login').Headers.["Token"] |> sprintf "Bearer %s" |> Authorization

let body resp' =
  match resp'.Body with
  | Binary content -> System.Text.Encoding.ASCII.GetString(content)
  | Text content -> content

let getIn (field' : string) resp' = JsonValue.Parse(resp').[field']
let getS (field' : string) resp' = (getIn field' resp').AsString()

let create (auth' : Auth) data' what' = 
  data' |> Json.serialize |> sendReq "POST" what' [ auth' ] |> body

let rec createGroup (auth' : Auth) (group' : Group) = 
  let group = { group' with kind= Some "O" } in
    let creator = create auth' group >> getS "id" in
      match group with
      | {team_id= None} -> 
        let tid = "/teams" |> mmApi |> creator |> Some in
          createGroup auth' {group with team_id= tid}
      | {team_id= Some _; channel_id= None} -> 
        let cid = "/channels" |> mmApi |> creator |> Some in
          createGroup auth' {group with channel_id= cid}
      | {team_id= Some _; channel_id= Some _} -> Json.serialize group

let createHook (auth' : Auth) (group' : Group) =
  create auth' group' (mmApi "/hooks/incoming")
  |> getS "id"

let createA = function
  | "group" -> createGroup
  | "hook" -> createHook
  | _ -> fun _ _ -> """"{"error": "no match"}"""

let (|Mention|_|) (name' : string) (msg' : string) =
  if msg'.Contains("@" + name') then
    Some msg'
  else
    None

let (|From|_|) (msg' : string) =
    let m = Regex.Match(msg', @"^(\w+):\n")
    if m.Success then 
      m.Groups.[0].Value.Split ':' |> Array.tryHead
    else None

let answer (state' : BotState) (post' : Post) =
  match post'.message with
  | Mention state'.name m & From f ->
    match m with
    | x when x.Contains("bye") -> "bye"
    | x when x.Contains("me too") -> sprintf "%s:\nnice, bye @%s" state'.name f
    | x when x.Contains("and you") -> sprintf "%s:\nme too, thanks @%s" state'.name f
    | x when x.Contains("how are you") -> sprintf "%s:\nim fine @%s\nand you?" state'.name f
    | x when x.Contains("hey") -> sprintf "%s:\nhi @%s\nhow are you?" state'.name f
    | _ -> ""
    |> fun msg' -> {| text= msg' |}
    |> Json.serialize
    |> sendReq "POST" (state'.hook |> sprintf "/hooks/%s" |> mmBase) [ContentType HttpContentTypes.Json]
    |> ignore
  | _ -> ()
  post'

let rec botLoop (state' : BotState) = async {
  do! Async.Sleep(1000)
  match state'.auth with
    | None -> return! botLoop {state' with auth= Some (getAuth state'.login)}
    | Some a -> 
      let posts' = match state'.last with 
                   | None -> getReq (sprintf "/channels/%s/posts" state'.group.channel_id.Value |> mmApi) [ a ]
                   | Some last -> getReq (sprintf "/channels/%s/posts?after=%s" state'.group.channel_id.Value last |> mmApi) [ a ]
                   |> body
                   |> Json.deserialize<Posts>
                   |> fun x -> Map.toSeq (x.posts)
      printfn "%A" posts'
      let last' = 
          match Seq.isEmpty posts' with
          | true ->  state'.last
          | false -> posts'
                     |> Seq.map snd
                     //|> Seq.filter (relevant state')
                     |> Seq.map (answer state')
                     |> Seq.maxBy (fun x -> x.update_at)
                     |> fun x -> x.id
                     |> Some
      return! botLoop { state' with last= last' }
}


[<EntryPoint>]
let main argv = 
  match argv with
  | [|"new"; what'; login'; group'|] ->
    group'
    |> Json.deserialize<Group>
    |> (createA what') (login' |> Json.deserialize<Login> |> getAuth)
    |> fun data' -> File.WriteAllText((sprintf ".tmp/%s.json" what'), data')
  | [|"bot"; hook'; login'; group'; bot1; bot2|] ->
    [bot1; bot2]
    |> List.map (newBot (Json.deserialize<Login> login') (Json.deserialize<Group> group') hook')
    |> List.map botLoop
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
  | _ -> ()
  0
