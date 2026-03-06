module Links

open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Giraffe
open Entity
open Mapping
open System.Linq
open Shared.SharedModels
open Dtos

let getLinks (db: AppDbContext) (userId: int) =
    task {
        let! profile = db.UserProfiles.FirstOrDefaultAsync(fun p -> p.UserId = userId)

        match if isNull profile then None else Some profile with
        | None -> return System.Collections.Generic.List<Entity.Link>()
        | Some profile ->
            let! links =
                db.Links
                    .AsQueryable()
                    .Where(fun (l: Entity.Link) -> l.UserProfileId = profile.Id)
                    .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
                    .ToListAsync()

            return links
    }

let saveLinks (db: AppDbContext) (userId: int) (linksDto: Link list) =
    task {
        let! userProfile =
            db.UserProfiles
                .FirstOrDefaultAsync(fun (p: Entity.UserProfile) -> p.UserId = userId)

        // If profile doesn't exist yet, create a minimal one so links can be saved.
        let userProfile, created =
            if isNull userProfile then
                let p = Entity.UserProfile()
                p.UserId <- userId
                p.FirstName <- ""
                p.LastName <- ""
                p.DisplayEmail <- null
                p.AvatarUrl <- null
                p.ProfileSlug <- sprintf "user-%d" userId
                db.UserProfiles.Add(p) |> ignore
                p, true
            else
                userProfile, false

        if created then
            let! _ = db.SaveChangesAsync()
            ()

        let! existingLinks =
            db.Links
                .Where(fun (l: Entity.Link) -> l.UserProfileId = userProfile.Id)
                .ToListAsync()

        db.Links.RemoveRange(existingLinks) |> ignore

        for linkDto in linksDto do
            let newLink = Entity.Link()
            newLink.Url <- linkDto.Url
            newLink.Platform <- toEntityPlatform linkDto.Platform
            newLink.SortOrder <- linkDto.SortOrder
            newLink.UserProfileId <- userProfile.Id
            db.Links.Add(newLink) |> ignore

        let! _ = db.SaveChangesAsync()
        return Ok()
    }

let getPreview (db: AppDbContext) (slug: string) =
    task {
        let! profile =
            db.UserProfiles
                .FirstOrDefaultAsync(fun p -> p.ProfileSlug = slug)

        match if isNull profile then None else Some profile with
        | None -> return None
        | Some profile ->
            let! links =
                db.Links
                    .Where(fun (l: Entity.Link) -> l.UserProfileId = profile.Id)
                    .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
                    .ToListAsync()

            return Some(profile, links)
    }

let private platformToString (p: Shared.SharedModels.Platform) : string =
    match p with
    | Shared.SharedModels.Platform.GitHub -> "GitHub"
    | Shared.SharedModels.Platform.Twitter -> "Twitter"
    | Shared.SharedModels.Platform.LinkedIn -> "LinkedIn"
    | Shared.SharedModels.Platform.YouTube -> "YouTube"
    | Shared.SharedModels.Platform.Facebook -> "Facebook"
    | Shared.SharedModels.Platform.Twitch -> "Twitch"
    | Shared.SharedModels.Platform.DevTo -> "DevTo"
    | Shared.SharedModels.Platform.CodeWars -> "CodeWars"
    | Shared.SharedModels.Platform.FreeCodeCamp -> "FreeCodeCamp"
    | Shared.SharedModels.Platform.GitLab -> "GitLab"
    | Shared.SharedModels.Platform.Hashnode -> "Hashnode"
    | Shared.SharedModels.Platform.StackOverflow -> "StackOverflow"
    | Shared.SharedModels.Platform.FrontendMentor -> "FrontendMentor"

let private platformOfString: string -> Shared.SharedModels.Platform =
    function
    | "GitHub" -> Shared.SharedModels.Platform.GitHub
    | "Twitter" -> Shared.SharedModels.Platform.Twitter
    | "LinkedIn" -> Shared.SharedModels.Platform.LinkedIn
    | "YouTube" -> Shared.SharedModels.Platform.YouTube
    | "Facebook" -> Shared.SharedModels.Platform.Facebook
    | "Twitch" -> Shared.SharedModels.Platform.Twitch
    | "DevTo" -> Shared.SharedModels.Platform.DevTo
    | "CodeWars" -> Shared.SharedModels.Platform.CodeWars
    | "FreeCodeCamp" -> Shared.SharedModels.Platform.FreeCodeCamp
    | "GitLab" -> Shared.SharedModels.Platform.GitLab
    | "Hashnode" -> Shared.SharedModels.Platform.Hashnode
    | "StackOverflow" -> Shared.SharedModels.Platform.StackOverflow
    | "FrontendMentor" -> Shared.SharedModels.Platform.FrontendMentor
    | _ -> Shared.SharedModels.Platform.GitHub

let handleGetLinks: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let userId = Auth.getUserId ctx
            let! links = getLinks db userId

            let linksDto: LinkOutputDto list =
                links
                |> Seq.map (fun l -> {
                    id = l.Id
                    platform = l.Platform |> toSharedPlatform |> platformToString
                    url = l.Url
                    sortOrder = l.SortOrder
                })
                |> List.ofSeq
            return! json linksDto next ctx
        }

let handleSaveLinks: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let userId = Auth.getUserId ctx
            // Bind to plain DTOs to avoid F# union deserialization issues
            let! payload = ctx.BindJsonAsync<LinkInputDto array>()
            let linksDto: Link list =
                payload
                |> Array.toList
                |> List.map (fun i -> {
                    Id = if i.id.HasValue then Some i.id.Value else None
                    Platform = platformOfString i.platform
                    Url = i.url
                    SortOrder = i.sortOrder
                })
            let! result = saveLinks db userId linksDto
            match result with
            | Ok() -> return! (setStatusCode 204 >=> text "") next ctx
            | Error msg -> return! (setStatusCode 400 >=> text msg) next ctx
        }

let handleGetPreview (slug: string) : HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let! preview = getPreview db slug
            match preview with
            | Some(p, links) ->
                let profileDto: ProfileOutputDto = {
                    firstName = if isNull p.FirstName then "" else p.FirstName
                    lastName = if isNull p.LastName then "" else p.LastName
                    displayEmail = if isNull p.DisplayEmail then "" else p.DisplayEmail
                    profileSlug = p.ProfileSlug
                    avatarUrl = if isNull p.AvatarUrl then "" else p.AvatarUrl
                }

                let linksDto: LinkOutputDto array =
                    links
                    |> Seq.map (fun l -> {
                        id = l.Id
                        platform = l.Platform |> toSharedPlatform |> platformToString
                        url = l.Url
                        sortOrder = l.SortOrder
                    })
                    |> Array.ofSeq
                // Return as an array [ profile, links ] for client compatibility
                let response: obj array = [| profileDto :> obj; linksDto :> obj |]
                return! json response next ctx
            | None -> return! (setStatusCode 404 >=> text "Preview not found") next ctx
        }
