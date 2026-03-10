module PreviewPage

open System
open Fable.Core
open Feliz
open Elmish
open FsToolkit.ErrorHandling
open Shared.SharedModels
open Shared.Api
open ClientShared
open Routing

type State =
  | Loading
  | Loaded of UserProfile * Link list
  | Error of string

type ShareState =
  | Idle
  | Copied
  | CopyFailed

type Model = {
  UserId: int
  PublicId: string option
  State: State
  ShareState: ShareState
}

type Msg =
  | Load
  | LoadResult of Result<UserProfile * Link list, AppError>
  | ShareLink of string
  | ShareLinkCopied
  | ShareLinkCopyFailed
  | ClearShareState

let private loadPreviewData (userId: int) =
  asyncResult {
    let! profile = ApiClient.ProfileApi.GetProfile userId
    let! links = ApiClient.LinkApi.GetLinks userId
    return profile, links
  }

[<Emit("navigator.clipboard.writeText($0)")>]
let private writeToClipboard (text: string) : JS.Promise<unit> = jsNative

let private copyToClipboard (text: string) = writeToClipboard text

let private clearShareStateCmd =
  Cmd.OfAsync.perform (fun () -> async {do! Async.Sleep 2500}) () (fun _ -> ClearShareState)

let private previewFullName (profile: UserProfile) =
  let full = (profile.FirstName + " " + profile.LastName).Trim ()
  if String.IsNullOrWhiteSpace full then "User" else full

let private mobileProfileLinksView (profile: UserProfile) (links: Link list) =
  Html.div [
    prop.className "w-[237px] mx-auto flex flex-col gap-14"
    prop.children [
      Html.div [
        prop.className "flex flex-col items-center gap-6"
        prop.children [
          match profile.AvatarUrl with
          | Some url when not (String.IsNullOrWhiteSpace url) ->
            Html.img [
              prop.src url
              prop.alt "Avatar"
              prop.className "w-[104px] h-[104px] rounded-full object-cover border-4 border-purple-600"
            ]
          | _ ->
            Html.div [
              prop.className "w-[104px] h-[104px] rounded-full bg-gray-200 border-4 border-purple-600"
            ]
          Html.div [
            prop.className "flex flex-col items-center gap-2"
            prop.children [
              Html.h1 [
                prop.className "text-preset-1 text-gray-900"
                prop.text (previewFullName profile)
              ]
              match profile.DisplayEmail with
              | Some em when em <> "" ->
                Html.p [
                  prop.className "text-preset-3-regular text-gray-500"
                  prop.text em
                ]
              | _ -> Html.none
            ]
          ]
        ]
      ]
      Html.ul [
        prop.className "w-full flex flex-col gap-6"
        prop.children [
          for l in links do
            Html.li [
              prop.key (string (defaultArg l.Id 0))
              prop.children [
                Ui.PlatformLink.view {Platform = l.Platform; Url = l.Url; Compact = false}
              ]
            ]
        ]
      ]
    ]
  ]

let init (userId: int) (publicId: string option) : Model * Cmd<Msg> =
  {
    UserId = userId
    PublicId = publicId
    State = Loading
    ShareState = Idle
  },
  Cmd.ofMsg Load

let update msg model : Model * Cmd<Msg> =
  match msg with
  | Load ->
    {model with State = Loading}, Cmd.OfAsync.either loadPreviewData model.UserId LoadResult (asUnexpected LoadResult)
  | LoadResult (Result.Ok (profile, links)) -> {model with State = Loaded (profile, links)}, Cmd.none
  | LoadResult (Result.Error err) -> {model with State = Error (appErrorToMessage err)}, Cmd.none
  | ShareLink link ->
    model, Cmd.OfPromise.either copyToClipboard link (fun _ -> ShareLinkCopied) (fun _ -> ShareLinkCopyFailed)
  | ShareLinkCopied -> {model with ShareState = Copied}, clearShareStateCmd
  | ShareLinkCopyFailed -> {model with ShareState = CopyFailed}, clearShareStateCmd
  | ClearShareState -> {model with ShareState = Idle}, Cmd.none

let view model dispatch onBackToEditor =
  let shareUrl =
    model.PublicId
    |> Option.filter (String.IsNullOrWhiteSpace >> not)
    |> Option.map (fun id -> absoluteUrl (PublicPreviewPage id))

  let content =
    match model.State with
    | Loading ->
      Html.p [
        prop.className "text-preset-3-regular text-gray-600 text-center"
        prop.text "Loading preview..."
      ]
    | Error e ->
      Html.p [
        prop.className "text-red-600 text-center"
        prop.text e
      ]
    | Loaded (profile, links) ->
      Html.div [
        prop.children [
          Html.div [
            prop.className "block md:hidden"
            prop.children [mobileProfileLinksView profile links]
          ]
          Html.div [
            prop.className "hidden md:block"
            prop.children [Ui.ProfileLinksView.view profile links]
          ]
        ]
      ]

  Html.div [
    prop.className "min-h-screen bg-white md:bg-gray-100 relative"
    prop.children [
      Html.div [
        prop.className "hidden md:block absolute inset-x-0 top-0 h-[357px] bg-purple-600 rounded-b-[32px]"
      ]
      Html.div [
        prop.className "relative z-10"
        prop.children [
          Html.div [
            prop.className "block md:hidden py-4 pl-6 pr-4"
            prop.children [
              Html.div [
                prop.className "flex items-center justify-between gap-4"
                prop.children [
                  Html.div [
                    prop.className "flex-1"
                    prop.children [
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Secondary
                        size = Ui.Button.Size.MdMobileFull
                        active = false
                        disabled = false
                        onClick = onBackToEditor
                        text = "Back to Editor"
                      |}
                    ]
                  ]
                  Html.div [
                    prop.className "flex-1"
                    prop.children [
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Primary
                        size = Ui.Button.Size.MdMobileFull
                        active = false
                        disabled = shareUrl.IsNone
                        onClick =
                          (fun () ->
                            match shareUrl with
                            | Some url -> dispatch (ShareLink url)
                            | None -> ()
                          )
                        text = "Share Link"
                      |}
                    ]
                  ]
                ]
              ]
            ]
          ]
          Html.div [
            prop.className "hidden md:block md:px-6 md:pt-6"
            prop.children [
              Html.div [
                prop.className "bg-white rounded-[12px] shadow-[var(--shadow-md)] py-4 pl-6 pr-4"
                prop.children [
                  Html.div [
                    prop.className "flex items-center justify-between gap-4"
                    prop.children [
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Secondary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = false
                        onClick = onBackToEditor
                        text = "Back to Editor"
                      |}
                      Ui.Button.view {|
                        variant = Ui.Button.Variant.Primary
                        size = Ui.Button.Size.Md
                        active = false
                        disabled = shareUrl.IsNone
                        onClick =
                          (fun () ->
                            match shareUrl with
                            | Some url -> dispatch (ShareLink url)
                            | None -> ()
                          )
                        text = "Share Link"
                      |}
                    ]
                  ]
                ]
              ]
            ]
          ]
          Html.div [
            prop.className "pb-12 pt-14 md:max-w-5xl md:mx-auto md:px-6 md:pt-16"
            prop.children [content]
          ]
        ]
      ]
      match model.ShareState with
      | Copied ->
        Ui.Toast.view {
          Message = "The link has been copied to your clipboard!"
          Variant = Ui.Toast.Variant.Success
          Icon = Some Ui.Icon.Name.LinkCopied
          Uppercase = true
        }
      | CopyFailed ->
        Ui.Toast.view {
          Message = "Could not copy link. Please try again."
          Variant = Ui.Toast.Variant.Error
          Icon = None
          Uppercase = false
        }
      | Idle -> Html.none
    ]
  ]
