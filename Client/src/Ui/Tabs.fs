module Ui.Tabs

open Feliz
open Routing

[<RequireQualifiedAccess>]
type TabId =
  | Links
  | Profile

let private label =
  function
  | TabId.Links -> "Links"
  | TabId.Profile -> "Profile Details"

let private iconFor =
  function
  | TabId.Links -> Ui.Icon.Name.LinksHeader
  | TabId.Profile -> Ui.Icon.Name.ProfileHeader

[<RequireQualifiedAccess>]
type Layout =
  | Compact
  | FullWidthSpaceBetween

type Props = {
  UserId: int
  Active: TabId
  OnSelect: TabId -> unit
  Layout: Layout
}

let private tabClasses (isActive: bool) =
  if isActive then
    "text-purple-600 bg-purple-300/20"
  else
    "text-gray-600 hover:text-gray-900 hover:bg-gray-100"

let view (p: Props) =
  let mkTab (id: TabId) =
    let isActive = id = p.Active
    Html.a [
      prop.href (
        match id with
        | TabId.Links -> href (UserLinksPage p.UserId)
        | TabId.Profile -> href (UserProfilePage p.UserId)
      )
      prop.onClick (fun _ -> p.OnSelect id)
      prop.className (
        "px-4 py-2 rounded-[var(--radius-md)] text-preset-3-semibold inline-flex items-center gap-2 "
        + tabClasses isActive
      )
      prop.children [
        Ui.Icon.view (iconFor id) (label id) (Some "w-4 h-4")
        Html.span [prop.text (label id)]
      ]
    ]

  Html.nav [
    prop.className (
      match p.Layout with
      | Layout.FullWidthSpaceBetween -> "flex w-full justify-between items-center gap-4"
      | Layout.Compact -> "flex gap-3"
    )
    prop.children [mkTab TabId.Links; mkTab TabId.Profile]
  ]
