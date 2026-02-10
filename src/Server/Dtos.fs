module Dtos

open System

[<CLIMutable>]
type LinkInputDto = {
    id: Nullable<int>
    platform: string
    url: string
    sortOrder: int
}

[<CLIMutable>]
type LinkOutputDto = {
    id: int
    platform: string
    url: string
    sortOrder: int
}

[<CLIMutable>]
type ProfileInputDto = {
    firstName: string
    lastName: string
    displayEmail: string
    profileSlug: string
    avatarUrl: string
}

[<CLIMutable>]
type ProfileOutputDto = {
    firstName: string
    lastName: string
    displayEmail: string
    profileSlug: string
    avatarUrl: string
}
