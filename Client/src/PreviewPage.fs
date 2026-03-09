module PreviewPage

open System
open Browser.Dom
open Fable.Core
open Feliz
open Elmish
open FsToolkit.ErrorHandling
open Shared.SharedModels
open Shared.Api
open ClientShared

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

let view model dispatch =
  let backToEditorPath = "#/user/" + string model.UserId + "/links"
  let shareUrl =
    model.PublicId
    |> Option.filter (String.IsNullOrWhiteSpace >> not)
    |> Option.map (fun id -> window.location.origin + "/#/" + id)

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
    | Loaded (profile, links) -> Ui.ProfileLinksView.view profile links

  Html.div [
    prop.className "min-h-screen bg-gray-100 relative"
    prop.children [
      Html.div [
        prop.className "hidden md:block absolute inset-x-0 top-0 h-[357px] bg-purple-600 rounded-b-[32px]"
      ]
      Html.div [
        prop.className "relative z-10"
        prop.children [
          Html.div [
            prop.className "max-w-5xl mx-auto px-4 pt-4 md:px-6 md:pt-6"
            prop.children [
              Html.div [
                prop.className
                  "bg-white rounded-[var(--radius-lg)] shadow-[var(--shadow-md)] p-4 flex items-center justify-between gap-4"
                prop.children [
                  Ui.Button.view {|
                    variant = Ui.Button.Variant.Secondary
                    size = Ui.Button.Size.Md
                    active = false
                    disabled = false
                    onClick = (fun () -> window.location.hash <- backToEditorPath)
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
          Html.div [
            prop.className "max-w-5xl mx-auto px-4 pb-12 pt-12 md:px-6 md:pt-16"
            prop.children [content]
          ]
        ]
      ]
      match model.ShareState with
      | Copied ->
        Html.div [
          prop.className
            "fixed bottom-6 left-1/2 -translate-x-1/2 bg-gray-900 text-white rounded-[var(--radius-md)] px-4 py-3 shadow-[var(--shadow-lg)] flex items-center gap-3"
          prop.children [
            Html.img [
              prop.src "/images/icon-link-copied-to-clipboard.svg"
              prop.alt "Copied"
            ]
            Html.p [
              prop.className "text-preset-4 uppercase tracking-[0.03em]"
              prop.text "The link has been copied to your clipboard!"
            ]
          ]
        ]
      | CopyFailed ->
        Html.div [
          prop.className
            "fixed bottom-6 left-1/2 -translate-x-1/2 bg-red-550 text-white rounded-[var(--radius-md)] px-4 py-3 shadow-[var(--shadow-lg)]"
          prop.text "Could not copy link. Please try again."
        ]
      | Idle -> Html.none
    ]
  ]
