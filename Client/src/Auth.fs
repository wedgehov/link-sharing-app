module Auth

open Elmish
open Shared.Api
open ClientShared

let appErrorToMessage = ClientShared.appErrorToMessage

let login (req: LoginRequest) (onResult: Result<User, AppError> -> 'msg) : Cmd<'msg> =
  Cmd.OfAsync.either ApiClient.AuthApi.Login req onResult (asUnexpected onResult)

let register (req: RegisterRequest) (onResult: Result<User, AppError> -> 'msg) : Cmd<'msg> =
  Cmd.OfAsync.either ApiClient.AuthApi.Register req onResult (asUnexpected onResult)

let logout (onResult: Result<unit, AppError> -> 'msg) : Cmd<'msg> =
  Cmd.OfAsync.either ApiClient.AuthApi.Logout () onResult (asUnexpected onResult)

let getCurrentUser (onResult: Result<User, AppError> -> 'msg) : Cmd<'msg> =
  Cmd.OfAsync.either ApiClient.AuthApi.GetCurrentUser () onResult (asUnexpected onResult)
