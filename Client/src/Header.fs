module Header

open Feliz
open Fable.Core.JsInterop
open Json
open Browser.Dom
open Routing // Import the new module

let private formatPath = List.toArray >> String.concat "/" >> (fun s -> "#/" + s)

let view (profileSlug: string option) =
  let env: obj = emitJsExpr () "import.meta.env"
  let v: obj = env?VITE_ENABLE_DEV_GALLERY
  let devLink =
    if isNull v || Json.isUndef v then
      Html.none
    else if unbox<string> v = "true" then
      Html.a [
        prop.className "text-preset-3-semibold text-purple-600 hover:text-purple-800"
        prop.href (DevGallery |> pageToPath |> formatPath)
        prop.text "Dev"
      ]
    else
      Html.none

  // Left group: brand (icon + text) and optional Dev link
  let leftGroup =
    let brand =
      Html.div [
        prop.className "flex items-center gap-2"
        prop.children [
          Html.img [
            prop.src "/images/logo-devlinks-small.svg"
            prop.alt "Devlinks"
          ]
          Html.span [
            prop.className "text-preset-3-semibold text-gray-900"
            prop.text "devlinks"
          ]
        ]
      ]
    Html.div [
      prop.className "flex items-center gap-4"
      prop.children [brand; devLink]
    ]

  // Middle group: compact paired tabs (Links | Profile Details)
  let centerGroup =
    // Determine active tab from current hash
    let currentHash = window.location.hash
    let isLinksActive =
      currentHash = (LinksPage |> pageToPath |> formatPath)
      || currentHash = "#/"
      || currentHash = ""
    let isProfileActive = currentHash = (ProfilePage |> pageToPath |> formatPath)

    let stateClasses isActive =
      if isActive then
        "text-purple-600 bg-purple-300/20"
      else
        "text-gray-600 hover:text-gray-900 hover:bg-gray-100"

    Html.div [
      prop.className "inline-flex gap-4"
      prop.children [
        Html.a [
          prop.className (
            "px-4 py-2 text-preset-3-semibold inline-flex items-center gap-2 rounded-[var(--radius-md)] "
            + stateClasses isLinksActive
          )
          prop.href (LinksPage |> pageToPath |> formatPath)
          prop.children [
            Ui.Icon.view Ui.Icon.Name.LinksHeader "Links" (Some "w-4 h-4")
            Html.span [prop.text "Links"]
          ]
        ]
        Html.a [
          prop.className (
            "px-4 py-2 text-preset-3-semibold inline-flex items-center gap-2 rounded-[var(--radius-md)] "
            + stateClasses isProfileActive
          )
          prop.href (ProfilePage |> pageToPath |> formatPath)
          prop.children [
            Ui.Icon.view Ui.Icon.Name.ProfileHeader "Profile Details" (Some "w-4 h-4")
            Html.span [prop.text "Profile Details"]
          ]
        ]
      ]
    ]

  // Right group: Preview tab
  let previewHref =
    match profileSlug with
    | Some s when not (System.String.IsNullOrWhiteSpace s) -> PreviewPage s |> pageToPath |> formatPath
    | _ -> PreviewPage "john-appleseed" |> pageToPath |> formatPath
  let rightGroup =
    Ui.Button.view {|
      variant = Ui.Button.Variant.Secondary
      size = Ui.Button.Size.Md
      active = false
      disabled = false
      onClick = (fun () -> window.location.hash <- previewHref)
      text = "Preview"
    |}

  Html.nav [
    prop.className "p-4 border-b bg-white flex items-center justify-between"
    prop.children [leftGroup; centerGroup; rightGroup]
  ]
