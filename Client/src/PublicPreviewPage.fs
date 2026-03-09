module PublicPreviewPage

open Feliz
open Elmish
open Shared.SharedModels
open Shared.Api
open ClientShared

type State =
  | Loading
  | Loaded of UserProfile * Link list
  | Error of string

type Model = {PublicId: string; State: State}

type Msg =
  | Load
  | LoadResult of Result<UserProfile * Link list, AppError>

let init (publicId: string) : Model * Cmd<Msg> =
  {PublicId = publicId; State = Loading}, Cmd.ofMsg Load

let update msg model : Model * Cmd<Msg> =
  match msg with
  | Load ->
    let load () =
      ApiClient.PublicApi.GetPreview model.PublicId
    {model with State = Loading}, Cmd.OfAsync.either load () LoadResult (asUnexpected LoadResult)
  | LoadResult (Result.Ok (profile, links)) -> {model with State = Loaded (profile, links)}, Cmd.none
  | LoadResult (Result.Error err) -> {model with State = Error (appErrorToMessage err)}, Cmd.none

let view model _dispatch =
  match model.State with
  | Loading -> Html.p "Loading public preview..."
  | Error e ->
    Html.p [
      prop.className "text-red-600"
      prop.text e
    ]
  | Loaded (profile, links) -> Ui.ProfileLinksView.view profile links
