module Shared.Api

open System
open FSharp.Control
open Shared.SharedModels

type ProfileApi = {
    GetProfile: unit -> Async<Result<UserProfile, string>>
    SaveProfile: UserProfile -> Async<Result<unit, string>>
}

type LinkApi = {
    GetLinks: unit -> Async<Result<Link list, string>>
    SaveLinks: Link list -> Async<Result<unit, string>>
}

type PublicApi = { GetPreview: string -> Async<Result<UserProfile * Link list, string>> }

type Api = {
    Profile: ProfileApi
    Links: LinkApi
    Public: PublicApi
}
