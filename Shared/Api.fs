module Shared.Api

open Shared.SharedModels

type AppError =
    | Unauthorized
    | NotFound
    | Conflict
    | InvalidCredentials
    | ValidationError of string
    | Unexpected of string

type User = { Id: int; Email: string }

type LoginRequest = { Email: string; Password: string }

type RegisterRequest = { Email: string; Password: string }

type IAuthApi = {
    Register: RegisterRequest -> Async<Result<User, AppError>>
    Login: LoginRequest -> Async<Result<User, AppError>>
    Logout: unit -> Async<Result<unit, AppError>>
    GetCurrentUser: unit -> Async<Result<User, AppError>>
}

type IProfileApi = {
    GetProfile: unit -> Async<Result<UserProfile, AppError>>
    SaveProfile: UserProfile -> Async<Result<unit, AppError>>
}

type ILinkApi = {
    GetLinks: unit -> Async<Result<Link list, AppError>>
    SaveLinks: Link list -> Async<Result<unit, AppError>>
}

type IPublicApi = { GetPreview: string -> Async<Result<UserProfile * Link list, AppError>> }
