module Header

open Feliz
open Feliz.Router
open Routing

let view (userId: int) (onNavigate: Page -> unit) =
  let linksPage = UserLinksPage userId
  let profilePage = UserProfilePage userId
  let previewPage = UserPreviewPage userId
  let linksPath = href linksPage
  let profilePath = href profilePage

  // Left group: brand (icon + text)
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
      prop.children [brand]
    ]

  // Middle group: compact paired tabs (Links | Profile Details)
  let centerGroup =
    let currentPage = Router.currentUrl () |> tryParsePath
    let isLinksActive =
      match currentPage with
      | UserLinksPage _ -> true
      | _ -> false
    let isProfileActive =
      match currentPage with
      | UserProfilePage _ -> true
      | _ -> false

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
          prop.href linksPath
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
          prop.href profilePath
          prop.children [
            Ui.Icon.view Ui.Icon.Name.ProfileHeader "Profile Details" (Some "w-4 h-4")
            Html.span [prop.text "Profile Details"]
          ]
        ]
      ]
    ]

  // Right group: Preview tab
  let rightGroup =
    Ui.Button.view {|
      variant = Ui.Button.Variant.Secondary
      size = Ui.Button.Size.Md
      active = false
      disabled = false
      onClick = (fun () -> onNavigate previewPage)
      text = "Preview"
    |}

  Html.nav [
    prop.className "p-4 border-b bg-white flex items-center justify-between"
    prop.children [leftGroup; centerGroup; rightGroup]
  ]
