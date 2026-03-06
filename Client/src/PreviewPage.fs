module PreviewPage

open Feliz
open Elmish
open Shared.SharedModels
open ApiClient

type State =
  | Loading
  | Loaded of UserProfile * Link list
  | Error of string

type Model = {Slug: string; State: State}

type Msg =
  | Load
  | LoadResult of Result<UserProfile * Link list, string>

let init (slug: string) : Model * Cmd<Msg> =
  {Slug = slug; State = Loading}, Cmd.ofMsg Load

let update msg model : Model * Cmd<Msg> =
  match msg with
  | Load ->
    let load () = api.Public.GetPreview model.Slug
    {model with State = Loading}, Cmd.OfAsync.either load () LoadResult (fun ex -> LoadResult (Result.Error ex.Message))
  | LoadResult (Result.Ok (profile, links)) -> {model with State = Loaded (profile, links)}, Cmd.none
  | LoadResult (Result.Error err) -> {model with State = Error err}, Cmd.none

let view model dispatch =
  match model.State with
  | Loading -> Html.p "Loading preview..."
  | Error e ->
    Html.p [
      prop.className "text-red-600"
      prop.text e
    ]
  | Loaded (profile, links) ->
    let fullName =
      let f, l = profile.FirstName, profile.LastName
      if
        System.String.IsNullOrWhiteSpace f
        && System.String.IsNullOrWhiteSpace l
      then
        profile.ProfileSlug
      else
        (f + " " + l).Trim ()
    Html.div [
      prop.className "max-w-md mx-auto p-6 flex flex-col gap-6"
      prop.children [
        Html.div [
          prop.className "flex items-center gap-4"
          prop.children [
            match profile.AvatarUrl with
            | Some url when not (System.String.IsNullOrWhiteSpace url) ->
              Html.img [
                prop.src url
                prop.alt "Avatar"
                prop.className "w-16 h-16 rounded-full object-cover"
              ]
            | _ ->
              Html.div [
                prop.className "w-16 h-16 rounded-full bg-gray-200"
              ]
            Html.div [
              prop.children [
                Html.h1 [
                  prop.className "text-preset-2"
                  prop.text fullName
                ]
                match profile.DisplayEmail with
                | Some em when em <> "" ->
                  Html.p [
                    prop.className "text-preset-4 text-gray-600"
                    prop.text em
                  ]
                | _ -> Html.none
              ]
            ]
          ]
        ]
        Html.ul [
          prop.className "flex flex-col gap-3"
          prop.children [
            for l in links do
              Html.li [
                prop.key (string (defaultArg l.Id 0))
                prop.children [
                  Ui.PlatformLink.view {Platform = l.Platform; Url = l.Url}
                ]
              ]
          ]
        ]
      ]
    ]
