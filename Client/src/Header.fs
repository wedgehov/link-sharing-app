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

  let leftGroup =
    Html.div [
      prop.className "flex items-center"
      prop.children [
        Html.img [
          prop.src "/images/logo-devlinks-small.svg"
          prop.alt "Devlinks"
          prop.className "w-8 h-8 md:hidden"
        ]
        Html.img [
          prop.src "/images/logo-devlinks-large.svg"
          prop.alt "Devlinks"
          prop.className "hidden md:block h-8 w-auto"
        ]
      ]
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

    let purpleIconFilter =
      "[filter:brightness(0)_saturate(100%)_invert(27%)_sepia(98%)_saturate(5360%)_hue-rotate(249deg)_brightness(101%)_contrast(101%)]"

    let stateClasses isActive =
      if isActive then
        "text-purple-600 bg-gray-100"
      else
        "text-gray-500 hover:text-gray-900 hover:bg-gray-100"

    Html.div [
      prop.className "inline-flex"
      prop.children [
        Html.a [
          prop.className (
            "px-6 py-4 text-preset-3-semibold inline-flex items-center gap-0 md:gap-2 rounded-[var(--radius-md)] transition-colors "
            + stateClasses isLinksActive
          )
          prop.href linksPath
          prop.children [
            Ui.Icon.view
              Ui.Icon.Name.LinksHeader
              "Links"
              (Some (
                if isLinksActive then
                  "w-5 h-5 " + purpleIconFilter
                else
                  "w-5 h-5"
              ))
            Html.span [
              prop.className "hidden md:inline"
              prop.text "Links"
            ]
          ]
        ]
        Html.a [
          prop.className (
            "px-6 py-4 text-preset-3-semibold inline-flex items-center gap-0 md:gap-2 rounded-[var(--radius-md)] transition-colors "
            + stateClasses isProfileActive
          )
          prop.href profilePath
          prop.children [
            Ui.Icon.view
              Ui.Icon.Name.ProfileHeader
              "Profile Details"
              (Some (
                if isProfileActive then
                  "w-5 h-5 " + purpleIconFilter
                else
                  "w-5 h-5"
              ))
            Html.span [
              prop.className "hidden md:inline"
              prop.text "Profile Details"
            ]
          ]
        ]
      ]
    ]

  let rightGroup =
    Html.button [
      prop.className
        "border border-purple-600 text-purple-600 text-preset-3-semibold rounded-[var(--radius-md)] inline-flex items-center justify-center p-4 md:px-6 md:py-4 cursor-pointer hover:bg-purple-300/20 transition-colors"
      prop.onClick (fun _ -> onNavigate previewPage)
      prop.children [
        Ui.Icon.view Ui.Icon.Name.PreviewHeader "Preview" (Some "w-5 h-5 md:hidden")
        Html.span [
          prop.className "hidden md:inline"
          prop.text "Preview"
        ]
      ]
    ]

  Html.nav [
    prop.className "bg-gray-50 md:px-6 md:pt-6"
    prop.children [
      Html.div [
        prop.className
          "bg-white rounded-[var(--radius-lg)] pl-6 pr-4 py-4 flex items-center justify-between gap-2 md:gap-4"
        prop.children [leftGroup; centerGroup; rightGroup]
      ]
    ]
  ]
