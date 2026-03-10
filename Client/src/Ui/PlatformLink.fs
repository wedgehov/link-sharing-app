module Ui.PlatformLink

open Feliz
open Shared.SharedModels
open System

let iconFor =
  function
  | Platform.GitHub -> Ui.Icon.Name.GitHub
  | Platform.Twitter -> Ui.Icon.Name.Twitter
  | Platform.LinkedIn -> Ui.Icon.Name.LinkedIn
  | Platform.YouTube -> Ui.Icon.Name.YouTube
  | Platform.Facebook -> Ui.Icon.Name.Facebook
  | Platform.Twitch -> Ui.Icon.Name.Twitch
  | Platform.DevTo -> Ui.Icon.Name.DevTo
  | Platform.CodeWars -> Ui.Icon.Name.CodeWars
  | Platform.FreeCodeCamp -> Ui.Icon.Name.FreeCodeCamp
  | Platform.GitLab -> Ui.Icon.Name.GitLab
  | Platform.Hashnode -> Ui.Icon.Name.Hashnode
  | Platform.StackOverflow -> Ui.Icon.Name.StackOverflow
  | Platform.FrontendMentor -> Ui.Icon.Name.FrontendMentor

let private labelFor =
  function
  | Platform.FreeCodeCamp -> "freeCodeCamp"
  | Platform.StackOverflow -> "Stack Overflow"
  | Platform.CodeWars -> "Codewars"
  | Platform.FrontendMentor -> "Frontend Mentor"
  | p -> string p

let private iconClassesFor =
  function
  | Platform.FrontendMentor -> "w-4 h-4"
  | _ -> "w-4 h-4 filter brightness-0 invert"

let private classesFor =
  function
  | Platform.GitHub -> "bg-gray-950 text-white"
  | Platform.YouTube -> "bg-red-550 text-white"
  | Platform.Twitter -> "bg-[#43b7e9] text-white"
  | Platform.LinkedIn -> "bg-blue-500 text-white"
  | Platform.Facebook -> "bg-[#2442ac] text-white"
  | Platform.Twitch -> "bg-[#ee3fc8] text-white"
  | Platform.DevTo -> "bg-gray-900 text-white"
  | Platform.CodeWars -> "bg-pink-900 text-white"
  | Platform.FreeCodeCamp -> "bg-[#302267] text-white"
  | Platform.GitLab -> "bg-[#eb4925] text-white"
  | Platform.Hashnode -> "bg-blue-800 text-white"
  | Platform.StackOverflow -> "bg-[#ec7100] text-white"
  | Platform.FrontendMentor -> "bg-white text-gray-950 border border-gray-300"

type Props = {Platform: Platform; Url: string; Compact: bool}

let private normalizeUrl (url: string) : string =
  if String.IsNullOrWhiteSpace url then
    "#"
  else
    let trimmed = url.Trim ()
    let lower = trimmed.ToLowerInvariant ()
    if
      lower.StartsWith ("http://")
      || lower.StartsWith ("https://")
    then
      trimmed
    else
      // Ensure we don't accidentally create https:////example when users prefix with '/'
      "https://" + trimmed.TrimStart ('/')

let view (p: Props) =
  let sizeClasses, iconClass, textClass =
    if p.Compact then
      "h-11 px-4", "w-4 h-4", "text-preset-4"
    else
      "px-4 py-4", "w-5 h-5", "text-preset-3-regular"

  let arrowClass =
    if p.Platform = Platform.FrontendMentor then
      "w-4 h-4 [filter:brightness(0)_saturate(100%)_invert(41%)_sepia(0%)_saturate(0%)_hue-rotate(183deg)_brightness(98%)_contrast(86%)]"
    else
      "w-4 h-4"

  Html.a [
    prop.href (normalizeUrl p.Url)
    prop.target "_blank"
    prop.rel "noopener noreferrer"
    prop.className (
      "w-full flex items-center justify-between rounded-[var(--radius-md)] "
      + sizeClasses
      + " "
      + classesFor p.Platform
    )
    prop.children [
      Html.div [
        prop.className "flex items-center gap-2"
        prop.children [
          Ui.Icon.view (iconFor p.Platform) (labelFor p.Platform) (Some (iconClass + " " + iconClassesFor p.Platform))
          Html.span [
            prop.className textClass
            prop.text (labelFor p.Platform)
          ]
        ]
      ]
      Ui.Icon.view Ui.Icon.Name.ArrowRight "Open" (Some arrowClass)
    ]
  ]
