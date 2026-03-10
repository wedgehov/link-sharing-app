module Ui.PhoneMockup

open Feliz
open Shared.SharedModels
open System

type Props = {Links: Link list; AvatarUrl: string option}

let view (p: Props) =
  Html.div [
    // Match the SVG's native geometry exactly to avoid sub-pixel drift.
    prop.className "relative w-[308px] h-[632px]"
    prop.children [
      Html.img [
        prop.src "/images/illustration-phone-mockup.svg"
        prop.alt "Phone preview"
        prop.className "absolute inset-0 w-full h-full select-none"
      ]

      // Avatar overlay positioned over the phone mockup's circular area
      // Align to mockup circle: top 63.5px, diameter 96px
      (match p.AvatarUrl with
       | Some url when not (String.IsNullOrWhiteSpace url) ->
         Html.img [
           prop.src url
           prop.alt "Avatar"
           prop.className
             // Centered horizontally; adjust top to align with the mockup circle.
             "absolute left-1/2 -translate-x-1/2 top-16 w-24 h-24 rounded-full object-cover shadow-[var(--shadow-sm)] pointer-events-none select-none z-10"
         ]
       | _ ->
         // Fallback placeholder if no avatar
         Html.div [
           prop.className "absolute left-1/2 -translate-x-1/2 top-16 w-24 h-24 rounded-full bg-gray-200 z-10"
         ])

      // Overlay stack of platform links positioned over the mock phone cards
      Html.div [
        prop.className (
          // Exact slot geometry from illustration-phone-mockup.svg:
          // x=35, y=278, width=237, height=44, vertical step=64.
          "absolute left-[35px] top-[278px] w-[237px] flex flex-col gap-[20px]"
        )
        prop.children [
          // Show up to five links and then fill missing slots with placeholders.
          let displayedLinks =
            p.Links
            |> List.sortBy (fun l -> l.SortOrder)
            |> List.truncate 5
          let placeholderCount = max 0 (5 - displayedLinks.Length)
          for l in displayedLinks do
            Ui.PlatformLink.view {Platform = l.Platform; Url = l.Url; Compact = true}
          for i in 1..placeholderCount do
            Html.div [
              prop.key (sprintf "placeholder-%d" i)
              prop.className "w-full h-11 rounded-[var(--radius-md)] bg-gray-100"
            ]
        ]
      ]
    ]
  ]
