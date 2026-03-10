module Validation

open System

let validateRequiredEmail (email: string) : Result<string, string> =
  let trimmed = email.Trim ()
  if String.IsNullOrWhiteSpace trimmed then
    Result.Error "Can't be empty"
  else
    let atIndex = trimmed.IndexOf '@'
    if atIndex <= 0 || atIndex >= trimmed.Length - 1 then
      Result.Error "Please check again"
    else
      let dotIndex = trimmed.IndexOf ('.', atIndex + 1)
      if
        dotIndex > atIndex + 1
        && dotIndex < trimmed.Length - 1
        && not (trimmed.Contains " ")
      then
        Result.Ok trimmed
      else
        Result.Error "Please check again"

let validateRequiredText (value: string) : Result<string, string> =
  let trimmed = value.Trim ()
  if String.IsNullOrWhiteSpace trimmed then
    Result.Error "Can't be empty"
  else
    Result.Ok trimmed
