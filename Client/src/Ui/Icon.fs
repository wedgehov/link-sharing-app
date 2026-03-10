module Ui.Icon

open Feliz

[<RequireQualifiedAccess>]
type Name =
  | GitHub
  | Twitter
  | LinkedIn
  | YouTube
  | Facebook
  | Twitch
  | DevTo
  | CodeWars
  | FreeCodeCamp
  | GitLab
  | Hashnode
  | StackOverflow
  | FrontendMentor
  | ChevronDown
  | Link
  | Email
  | Password
  | UploadImage
  | ArrowRight
  | ChangesSaved
  | LinkCopied
  | LinksHeader
  | ProfileHeader
  | PreviewHeader

let private pathFor =
  function
  | Name.GitHub -> "/images/icon-github.svg"
  | Name.Twitter -> "/images/icon-twitter.svg"
  | Name.LinkedIn -> "/images/icon-linkedin.svg"
  | Name.YouTube -> "/images/icon-youtube.svg"
  | Name.Facebook -> "/images/icon-facebook.svg"
  | Name.Twitch -> "/images/icon-twitch.svg"
  | Name.DevTo -> "/images/icon-devto.svg"
  | Name.CodeWars -> "/images/icon-codewars.svg"
  | Name.FreeCodeCamp -> "/images/icon-freecodecamp.svg"
  | Name.GitLab -> "/images/icon-gitlab.svg"
  | Name.Hashnode -> "/images/icon-hashnode.svg"
  | Name.StackOverflow -> "/images/icon-stack-overflow.svg"
  | Name.FrontendMentor -> "/images/icon-frontend-mentor.svg"
  | Name.ChevronDown -> "/images/icon-chevron-down.svg"
  | Name.Link -> "/images/icon-link.svg"
  | Name.Email -> "/images/icon-email.svg"
  | Name.Password -> "/images/icon-password.svg"
  | Name.UploadImage -> "/images/icon-upload-image.svg"
  | Name.ArrowRight -> "/images/icon-arrow-right.svg"
  | Name.ChangesSaved -> "/images/icon-changes-saved.svg"
  | Name.LinkCopied -> "/images/icon-link-copied-to-clipboard.svg"
  | Name.LinksHeader -> "/images/icon-links-header.svg"
  | Name.ProfileHeader -> "/images/icon-profile-details-header.svg"
  | Name.PreviewHeader -> "/images/icon-preview-header.svg"

let view (name: Name) (altText: string) (classes: string option) =
  Html.img [
    prop.src (pathFor name)
    prop.alt altText
    match classes with
    | Some c -> prop.className c
    | None -> ()
  ]
