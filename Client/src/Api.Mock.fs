module Api.Mock

open Shared.Api
open Shared.SharedModels
open FSharp.Control

let private mockProfile: UserProfile = {
  FirstName = "John"
  LastName = "Appleseed"
  DisplayEmail = Some "john.appleseed@example.com"
  ProfileSlug = "john-appleseed"
  AvatarUrl = None
}

let private mockLinks: Link list = [
  {
    Id = Some 1
    Platform = Platform.GitHub
    Url = "https://github.com"
    SortOrder = 1
  }
  {
    Id = Some 2
    Platform = Platform.YouTube
    Url = "https://youtube.com"
    SortOrder = 2
  }
]

let private profileApi: ProfileApi = {
  GetProfile = fun () -> async {return Ok mockProfile}
  SaveProfile = fun _ -> async {return Ok ()}
}

let private linkApi: LinkApi = {
  GetLinks = fun () -> async {return Ok mockLinks}
  SaveLinks = fun _ -> async {return Ok ()}
}

let private publicApi: PublicApi = {GetPreview = fun _ -> async {return Ok (mockProfile, mockLinks)}}

let api: Api = {Profile = profileApi; Links = linkApi; Public = publicApi}
