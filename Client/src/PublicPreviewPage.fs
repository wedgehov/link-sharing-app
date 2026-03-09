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
  let content =
    match model.State with
    | Loading ->
      Html.p [
        prop.className "text-preset-3-regular text-gray-600 text-center"
        prop.text "Loading public preview..."
      ]
    | Error e ->
      Html.p [
        prop.className "text-red-600 text-center"
        prop.text e
      ]
    | Loaded (profile, links) -> Ui.ProfileLinksView.view profile links

  Html.div [
    prop.className "min-h-screen bg-gray-100 relative"
    prop.children [
      Html.div [
        prop.className "hidden md:block absolute inset-x-0 top-0 h-[357px] bg-purple-600 rounded-b-[32px]"
      ]
      Html.div [
        prop.className "relative z-10 max-w-5xl mx-auto px-4 pt-12 pb-12 md:px-6 md:pt-16"
        prop.children [content]
      ]
    ]
  ]
