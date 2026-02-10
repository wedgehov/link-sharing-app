module ApiClient

open Shared.Api
open FSharp.Control
open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleHttp
open Json
open Shared.SharedModels

let private useMockApi () =
  let env: obj = emitJsExpr () "import.meta.env"
  let v: obj = env?VITE_USE_MOCK_API
  if isNull v || isUndef v then
    false
  else
    unbox<string> v = "true"

let private is2xx (status: int) = status >= 200 && status < 300
let private bodyOrEmpty (s: string) = if isNull s then "" else s

let private platformFromInt (n: int) : Platform =
  match n with
  | 0 -> Platform.GitHub
  | 1 -> Platform.Twitter
  | 2 -> Platform.LinkedIn
  | 3 -> Platform.YouTube
  | 4 -> Platform.Facebook
  | 5 -> Platform.Twitch
  | 6 -> Platform.DevTo
  | 7 -> Platform.CodeWars
  | 8 -> Platform.FreeCodeCamp
  | 9 -> Platform.GitLab
  | 10 -> Platform.Hashnode
  | 11 -> Platform.StackOverflow
  | 12 -> Platform.FrontendMentor
  | _ -> Platform.GitHub

let private platformFromString (s: string) : Platform =
  match s with
  | null -> Platform.GitHub
  | _ ->
    match s with
    | "GitHub" -> Platform.GitHub
    | "Twitter" -> Platform.Twitter
    | "LinkedIn" -> Platform.LinkedIn
    | "YouTube" -> Platform.YouTube
    | "Facebook" -> Platform.Facebook
    | "Twitch" -> Platform.Twitch
    | "DevTo" -> Platform.DevTo
    | "CodeWars" -> Platform.CodeWars
    | "FreeCodeCamp" -> Platform.FreeCodeCamp
    | "GitLab" -> Platform.GitLab
    | "Hashnode" -> Platform.Hashnode
    | "StackOverflow" -> Platform.StackOverflow
    | "FrontendMentor" -> Platform.FrontendMentor
    | _ -> Platform.GitHub

let private parsePlatform (o: obj) : Platform =
  if isNull o || isUndef o then
    Platform.GitHub
  else
    let ty: string = emitJsExpr o "typeof $0"
    if ty = "string" then
      platformFromString (unbox<string> o)
    elif ty = "number" then
      platformFromInt (int (unbox<float> o))
    else
      // Try object with Case property: { "Case": "GitHub" }
      let caseName = getFirst<string> o ["Case"; "case"; "Tag"; "tag"]
      match caseName with
      | Some s -> platformFromString s
      | None -> Platform.GitHub

let private parseLinkObj (o: obj) : Link =
  let idOpt = getFirst<int> o ["id"; "Id"] |> Option.map Some |> Option.defaultValue None
  let platformObj: obj =
    match getFirst<obj> o ["platform"; "Platform"] with
    | Some p -> p
    | None -> null
  let url = getFirst<string> o ["url"; "Url"] |> Option.defaultValue ""
  let sortOrder = getFirst<int> o ["sortOrder"; "SortOrder"] |> Option.defaultValue 0
  {
    Id = idOpt
    Platform = parsePlatform platformObj
    Url = url
    SortOrder = sortOrder
  }

let private parseUserProfileObj (o: obj) : UserProfile =
  let firstName = getFirst<string> o ["firstName"; "FirstName"] |> Option.defaultValue ""
  let lastName = getFirst<string> o ["lastName"; "LastName"] |> Option.defaultValue ""
  let displayEmail = getFirst<string> o ["displayEmail"; "DisplayEmail"]
  let profileSlug = getFirst<string> o ["profileSlug"; "ProfileSlug"] |> Option.defaultValue ""
  let avatarUrl = getFirst<string> o ["avatarUrl"; "AvatarUrl"]
  {
    FirstName = firstName
    LastName = lastName
    DisplayEmail = displayEmail
    ProfileSlug = profileSlug
    AvatarUrl = avatarUrl
  }

let private parseLinksJson (json: string) : Result<Link list, exn> =
  try
    let arr: obj array = unbox (JS.JSON.parse json)
    let links = arr |> Array.toList |> List.map parseLinkObj
    Ok links
  with e ->
    Error (exn ($"Failed to parse Links JSON: {e.Message}"))

let private parseProfileJson (json: string) : Result<UserProfile, exn> =
  try
    Ok (JS.JSON.parse json |> parseUserProfileObj)
  with e ->
    Error (exn ($"Failed to parse UserProfile JSON: {e.Message}"))

let private parsePreviewJson (json: string) : Result<UserProfile * Link list, exn> =
  try
    // Accept either [profile, links] or { Item1 = profile; Item2 = links }
    let root: obj = JS.JSON.parse json
    let profileObjOpt =
      match getFirst<obj> root ["Item1"; "item1"] with
      | Some p -> Some p
      | None ->
        let ty: string = emitJsExpr root "Array.isArray($0) ? 'array' : typeof $0"
        if ty = "array" then
          let arr: obj array = unbox root
          if arr.Length >= 1 then Some arr.[0] else None
        else None
    let linksObjOpt =
      match getFirst<obj> root ["Item2"; "item2"] with
      | Some l -> Some l
      | None ->
        let ty: string = emitJsExpr root "Array.isArray($0) ? 'array' : typeof $0"
        if ty = "array" then
          let arr: obj array = unbox root
          if arr.Length >= 2 then Some arr.[1] else None
        else None
    match profileObjOpt, linksObjOpt with
    | Some p, Some l ->
      let profile = parseUserProfileObj p
      // links array
      let linksArr: obj array = unbox l
      let links = linksArr |> Array.toList |> List.map parseLinkObj
      Ok (profile, links)
    | _ -> Error (exn "Unexpected preview JSON shape")
  with e ->
    Error (exn ($"Failed to parse Preview JSON: {e.Message}"))

let private realProfileApi: ProfileApi = {
  GetProfile = fun () ->
    async {
      let! res =
        Http.request "/api/profile"
        |> Http.method GET
        |> Http.send
      let status = res.statusCode
      let body = bodyOrEmpty res.responseText
      if not (is2xx status) then
        return Error (sprintf "HTTP %d: %s" status body)
      else
        match parseProfileJson body with
        | Ok p -> return Ok p
        | Error ex -> return Error ex.Message
    }
  SaveProfile = fun (profile: UserProfile) ->
    async {
      let payload =
        createObj [
          "firstName" ==> profile.FirstName
          "lastName" ==> profile.LastName
          "displayEmail" ==> (defaultArg profile.DisplayEmail null)
          "profileSlug" ==> profile.ProfileSlug
          "avatarUrl" ==> (defaultArg profile.AvatarUrl null)
        ]
      let! res =
        Http.request "/api/profile"
        |> Http.method PUT
        |> Http.header (Headers.contentType "application/json")
        |> Http.content (BodyContent.Text (JS.JSON.stringify payload))
        |> Http.send
      if res.statusCode = 204 || (is2xx res.statusCode && bodyOrEmpty res.responseText = "") then
        return Ok ()
      else
        return Error (sprintf "HTTP %d: %s" res.statusCode (bodyOrEmpty res.responseText))
    }
}

let private realLinkApi: LinkApi = {
  GetLinks = fun () ->
    async {
      let! res =
        Http.request "/api/links"
        |> Http.method GET
        |> Http.send
      let status = res.statusCode
      let body = bodyOrEmpty res.responseText
      if not (is2xx status) then
        return Error (sprintf "HTTP %d: %s" status body)
      else
        match parseLinksJson body with
        | Ok xs -> return Ok xs
        | Error ex -> return Error ex.Message
    }
  SaveLinks = fun (links: Link list) ->
    async {
      let payload =
        links
        |> List.map (fun l ->
          createObj [
            // Id is optional in payload; backend recreates the set
            // For completeness, include when present
            match l.Id with
            | Some id -> "id" ==> id
            | None -> ()
            "platform" ==> (string l.Platform)
            "url" ==> l.Url
            "sortOrder" ==> l.SortOrder
          ])
        |> List.toArray
        |> box
      let! res =
        Http.request "/api/links"
        |> Http.method PUT
        |> Http.header (Headers.contentType "application/json")
        |> Http.content (BodyContent.Text (JS.JSON.stringify payload))
        |> Http.send
      if res.statusCode = 204 || (is2xx res.statusCode && bodyOrEmpty res.responseText = "") then
        return Ok ()
      else
        return Error (sprintf "HTTP %d: %s" res.statusCode (bodyOrEmpty res.responseText))
    }
}

let private realPublicApi: PublicApi = {
  GetPreview = fun (slug: string) ->
    async {
      let! res =
        Http.request ($"/api/public/preview/{slug}")
        |> Http.method GET
        |> Http.send
      let status = res.statusCode
      let body = bodyOrEmpty res.responseText
      if not (is2xx status) then
        return Error (sprintf "HTTP %d: %s" status body)
      else
        match parsePreviewJson body with
        | Ok x -> return Ok x
        | Error ex -> return Error ex.Message
    }
}

let private realApi: Api = { Profile = realProfileApi; Links = realLinkApi; Public = realPublicApi }

let api: Api =
  if useMockApi () then
    printfn "Using Mock API"
    Api.Mock.api
  else
    printfn "Using Real API"
    realApi
