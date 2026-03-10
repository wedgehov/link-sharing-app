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
    prop.className
      "w-full max-w-sm mx-auto bg-white rounded-[24px] shadow-[var(--shadow-lg)] px-6 py-10 md:px-12 md:py-12 flex flex-col items-center gap-8"
    prop.children [
      Html.div [
        prop.className "flex flex-col items-center gap-6"
        prop.children [
          match profile.AvatarUrl with
          | Some url when not (System.String.IsNullOrWhiteSpace url) ->
            Html.img [
              prop.src url
              prop.alt "Avatar"
              prop.className "w-24 h-24 rounded-full object-cover border-4 border-purple-600"
            ]
          | _ ->
            Html.div [
              prop.className "w-24 h-24 rounded-full bg-gray-200 border-4 border-purple-600"
            ]
          Html.div [
            prop.className "text-center"
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
        prop.className "w-full flex flex-col gap-4"
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
