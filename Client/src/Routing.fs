module Routing

open System

type Page =
  | LoginPage
  | RegisterPage
  | UserLinksPage of userId: int
  | UserProfilePage of userId: int
  | UserPreviewPage of userId: int
  | PublicPreviewPage of publicId: string

let private tryParseInt (value: string) =
  match Int32.TryParse value with
  | true, parsed -> Some parsed
  | _ -> None

let private isGuid (value: string) =
  match Guid.TryParse value with
  | true, _ -> true
  | _ -> false

let pageToPath (page: Page) : string list =
  match page with
  | LoginPage -> ["login"]
  | RegisterPage -> ["register"]
  | UserLinksPage userId -> ["user"; string userId; "links"]
  | UserProfilePage userId -> ["user"; string userId; "profile"]
  | UserPreviewPage userId -> ["user"; string userId; "preview"]
  | PublicPreviewPage publicId -> [publicId]

let pathParser (path: string list) : Page =
  match path with
  | []
  | [""] -> LoginPage
  | ["login"] -> LoginPage
  | ["register"] -> RegisterPage
  | ["user"; userId; "links"] ->
    match tryParseInt userId with
    | Some id -> UserLinksPage id
    | None -> LoginPage
  | ["user"; userId; "profile"] ->
    match tryParseInt userId with
    | Some id -> UserProfilePage id
    | None -> LoginPage
  | ["user"; userId; "preview"] ->
    match tryParseInt userId with
    | Some id -> UserPreviewPage id
    | None -> LoginPage
  | [single] when isGuid single -> PublicPreviewPage single
  | _ -> LoginPage
