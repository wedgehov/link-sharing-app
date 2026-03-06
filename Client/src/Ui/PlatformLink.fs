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
  | Platform.FrontendMentor -> "w-5 h-5"
  | _ -> "w-5 h-5 filter brightness-0 invert"

let private classesFor =
  function
  | Platform.GitHub -> "bg-gray-950 text-white"
  | Platform.YouTube -> "bg-red-500 text-white"
  | Platform.Twitter -> "bg-blue-500 text-white"
  | Platform.LinkedIn -> "bg-blue-800 text-white"
  | Platform.Facebook -> "bg-blue-500 text-white"
  | Platform.Twitch -> "bg-purple-600 text-white"
  | Platform.DevTo -> "bg-gray-950 text-white"
  | Platform.CodeWars -> "bg-orange-600 text-white"
  | Platform.FreeCodeCamp -> "bg-purple-950 text-white"
  | Platform.GitLab -> "bg-orange-500 text-white"
  | Platform.Hashnode -> "bg-pink-400 text-white"
  | Platform.StackOverflow -> "bg-orange-600 text-white"
  | Platform.FrontendMentor -> "bg-white text-gray-950 border border-gray-300"

type Props = {Platform: Platform; Url: string}

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
  Html.a [
    prop.href (normalizeUrl p.Url)
    prop.target "_blank"
    prop.rel "noopener noreferrer"
    prop.className (
      "w-full flex items-center justify-between px-4 py-3 rounded-[var(--radius-md)] "
      + classesFor p.Platform
    )
    prop.children [
      Html.div [
        prop.className "flex items-center gap-3"
        prop.children [
          Ui.Icon.view (iconFor p.Platform) (labelFor p.Platform) (Some (iconClassesFor p.Platform))
          Html.span [
            prop.className "text-preset-3-semibold"
            prop.text (labelFor p.Platform)
          ]
        ]
      ]
      Ui.Icon.view Ui.Icon.Name.ArrowRight "Open" (Some "w-4 h-4")
    ]
  ]
