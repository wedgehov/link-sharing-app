module Mapping

open Entity
open Shared

let toEntityPlatform (p: Shared.SharedModels.Platform) : Entity.Platform =
    match p with
    | Shared.SharedModels.Platform.GitHub -> Entity.Platform.GitHub
    | Shared.SharedModels.Platform.Twitter -> Entity.Platform.Twitter
    | Shared.SharedModels.Platform.LinkedIn -> Entity.Platform.LinkedIn
    | Shared.SharedModels.Platform.YouTube -> Entity.Platform.YouTube
    | Shared.SharedModels.Platform.Facebook -> Entity.Platform.Facebook
    | Shared.SharedModels.Platform.Twitch -> Entity.Platform.Twitch
    | Shared.SharedModels.Platform.DevTo -> Entity.Platform.DevTo
    | Shared.SharedModels.Platform.CodeWars -> Entity.Platform.CodeWars
    | Shared.SharedModels.Platform.FreeCodeCamp -> Entity.Platform.FreeCodeCamp
    | Shared.SharedModels.Platform.GitLab -> Entity.Platform.GitLab
    | Shared.SharedModels.Platform.Hashnode -> Entity.Platform.Hashnode
    | Shared.SharedModels.Platform.StackOverflow -> Entity.Platform.StackOverflow
    | Shared.SharedModels.Platform.FrontendMentor -> Entity.Platform.FrontendMentor

let toSharedPlatform (p: Entity.Platform) : Shared.SharedModels.Platform =
    match p with
    | Entity.Platform.GitHub -> Shared.SharedModels.Platform.GitHub
    | Entity.Platform.Twitter -> Shared.SharedModels.Platform.Twitter
    | Entity.Platform.LinkedIn -> Shared.SharedModels.Platform.LinkedIn
    | Entity.Platform.YouTube -> Shared.SharedModels.Platform.YouTube
    | Entity.Platform.Facebook -> Shared.SharedModels.Platform.Facebook
    | Entity.Platform.Twitch -> Shared.SharedModels.Platform.Twitch
    | Entity.Platform.DevTo -> Shared.SharedModels.Platform.DevTo
    | Entity.Platform.CodeWars -> Shared.SharedModels.Platform.CodeWars
    | Entity.Platform.FreeCodeCamp -> Shared.SharedModels.Platform.FreeCodeCamp
    | Entity.Platform.GitLab -> Shared.SharedModels.Platform.GitLab
    | Entity.Platform.Hashnode -> Shared.SharedModels.Platform.Hashnode
    | Entity.Platform.StackOverflow -> Shared.SharedModels.Platform.StackOverflow
    | Entity.Platform.FrontendMentor -> Shared.SharedModels.Platform.FrontendMentor
    | _ -> failwith "Unknown platform enum value encountered"
