module Shared.SharedModels

type Platform =
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

[<CLIMutable>]
type Link = {
    Id: int option
    Platform: Platform
    Url: string
    SortOrder: int
}

[<CLIMutable>]
type UserProfile = {
    FirstName: string
    LastName: string
    DisplayEmail: string option
    ProfileSlug: string
    AvatarUrl: string option
}
