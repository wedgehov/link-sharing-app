module Routing

open System
open Browser.Dom
open Elmish
open Feliz.Router

type Page =
  | LoginPage
  | RegisterPage
  | UserLinksPage of userId: int
  | UserProfilePage of userId: int
  | UserPreviewPage of userId: int
  | PublicPreviewPage of publicId: string
  | NotFoundPage

let pageToPath (page: Page) : string list =
  match page with
  | LoginPage -> ["login"]
  | RegisterPage -> ["register"]
  | UserLinksPage userId -> ["user"; string userId; "links"]
  | UserProfilePage userId -> ["user"; string userId; "profile"]
  | UserPreviewPage userId -> ["user"; string userId; "preview"]
  | PublicPreviewPage publicId -> [publicId]
  | NotFoundPage -> ["404"]

let tryParsePath (path: string list) : Page =
  match path with
  | []
  | [""] -> LoginPage
  | ["login"] -> LoginPage
  | ["register"] -> RegisterPage
  | ["404"] -> NotFoundPage
  | ["user"; Route.Int userId; "links"] -> UserLinksPage userId
  | ["user"; Route.Int userId; "profile"] -> UserProfilePage userId
  | ["user"; Route.Int userId; "preview"] -> UserPreviewPage userId
  | [publicId] ->
    match Guid.TryParse publicId with
    | true, _ -> PublicPreviewPage publicId
    | _ -> NotFoundPage
  | _ -> NotFoundPage

let tryParseCurrentUrl () = Router.currentUrl () |> tryParsePath

let href (page: Page) : string = page |> pageToPath |> Router.format

let absoluteUrl (page: Page) : string =
  window.location.origin + "/" + href page

let navigateCmd (page: Page) : Cmd<'msg> =
  page |> pageToPath |> List.toArray |> Cmd.navigate
