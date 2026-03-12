module Shared.Api

open Shared.SharedModels

type AppError =
    | Unauthorized
    | NotFound
    | Conflict
    | InvalidCredentials
    | ValidationError of string
    | Unexpected of string

[<RequireQualifiedAccess>]
type UserRole =
    | Standard
    | Admin

type User = {
    Id: int
    Email: string
    PublicId: string
    Role: UserRole
}

type LoginRequest = { Email: string; Password: string }

type RegisterRequest = { Email: string; Password: string }

type IAuthApi = {
    Register: RegisterRequest -> Async<Result<User, AppError>>
    Login: LoginRequest -> Async<Result<User, AppError>>
    Logout: unit -> Async<Result<unit, AppError>>
    GetCurrentUser: unit -> Async<Result<User, AppError>>
}

type IProfileApi = {
    GetProfile: int -> Async<Result<UserProfile, AppError>>
    SaveProfile: int -> ProfileDetails -> Async<Result<unit, AppError>>
}

type ILinkApi = {
    GetLinks: int -> Async<Result<Link list, AppError>>
    SaveLinks: int -> Link list -> Async<Result<unit, AppError>>
}

type IPublicApi = { GetPreview: string -> Async<Result<UserProfile * Link list, AppError>> }
