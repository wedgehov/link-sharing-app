module ClientShared

open Shared.Api

let asUnexpected wrap (ex: exn) =
  ex.Message |> Unexpected |> Error |> wrap

let appErrorToMessage (error: AppError) =
  match error with
  | Unauthorized -> "You are not authenticated. Please log in."
  | NotFound -> "The requested resource was not found."
  | Conflict -> "A user with that email already exists."
  | InvalidCredentials -> "Invalid credentials."
  | ValidationError message -> message
  | Unexpected message -> message
