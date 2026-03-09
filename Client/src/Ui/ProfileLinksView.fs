module Ui.ProfileLinksView

open Feliz
open Shared.SharedModels

let view (profile: UserProfile) (links: Link list) =
  let fullName =
    let f, l = profile.FirstName, profile.LastName
    let full = (f + " " + l).Trim ()
    if System.String.IsNullOrWhiteSpace full then
      "User"
    else
      full

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
