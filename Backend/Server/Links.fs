module Links

open FsToolkit.ErrorHandling
open System.Linq
open Entity
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Shared.SharedModels
open Shared.Api
open Mapping

let getLinks (db: AppDbContext) (userId: int) =
    taskResult {
        let! profile =
            db.UserProfiles.FirstOrDefaultAsync(fun p -> p.UserId = userId)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj

        match profile with
        | None -> return System.Collections.Generic.List<Entity.Link>()
        | Some profile ->
            return!
                db.Links
                    .AsQueryable()
                    .Where(fun (l: Entity.Link) -> l.UserProfileId = profile.Id)
                    .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
                    .ToListAsync()
                |> TaskResult.ofTask
    }

let saveLinks (db: AppDbContext) (userId: int) (linksDto: Link list) =
    taskResult {
        let! userProfile =
            db.UserProfiles.FirstOrDefaultAsync(fun (p: Entity.UserProfile) -> p.UserId = userId)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj

        // If profile doesn't exist yet, create a minimal one so links can be saved.
        let! userProfile =
            match userProfile with
            | None ->
                taskResult {
                    let p = Entity.UserProfile()
                    p.UserId <- userId
                    p.FirstName <- ""
                    p.LastName <- ""
                    p.DisplayEmail <- null
                    p.AvatarUrl <- null
                    p.ProfileSlug <- sprintf "user-%d" userId
                    db.UserProfiles.Add(p) |> ignore
                    do! db.SaveChangesAsync() |> TaskResult.ofTask |> TaskResult.ignore
                    return p
                }
            | Some profile -> TaskResult.ofResult (Ok profile)

        let! existingLinks =
            db.Links.Where(fun (l: Entity.Link) -> l.UserProfileId = userProfile.Id).ToListAsync()
            |> TaskResult.ofTask

        db.Links.RemoveRange(existingLinks) |> ignore

        for linkDto in linksDto do
            let newLink = Entity.Link()
            newLink.Url <- linkDto.Url
            newLink.Platform <- toEntityPlatform linkDto.Platform
            newLink.SortOrder <- linkDto.SortOrder
            newLink.UserProfileId <- userProfile.Id
            db.Links.Add(newLink) |> ignore

        do! db.SaveChangesAsync() |> TaskResult.ofTask |> TaskResult.ignore
    }

let getPreview (db: AppDbContext) (slug: string) =
    taskResult {
        let! profile =
            db.UserProfiles.FirstOrDefaultAsync(fun p -> p.ProfileSlug = slug)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj
            |> TaskResult.bind (Result.requireSome NotFound >> TaskResult.ofResult)

        let! links =
            db.Links
                .Where(fun (l: Entity.Link) -> l.UserProfileId = profile.Id)
                .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
                .ToListAsync()
            |> TaskResult.ofTask

        return profile, links
    }

let private toSharedLink (link: Entity.Link) : Link = {
    Id = Some link.Id
    Platform = toSharedPlatform link.Platform
    Url = link.Url
    SortOrder = link.SortOrder
}

let private toSharedProfile (profile: Entity.UserProfile) : UserProfile = {
    FirstName = if isNull profile.FirstName then "" else profile.FirstName
    LastName = if isNull profile.LastName then "" else profile.LastName
    DisplayEmail =
        if
            isNull profile.DisplayEmail
            || System.String.IsNullOrWhiteSpace profile.DisplayEmail
        then
            None
        else
            Some profile.DisplayEmail
    ProfileSlug =
        if isNull profile.ProfileSlug then
            ""
        else
            profile.ProfileSlug
    AvatarUrl =
        if isNull profile.AvatarUrl || System.String.IsNullOrWhiteSpace profile.AvatarUrl then
            None
        else
            Some profile.AvatarUrl
}

let linksApiImplementation (ctx: HttpContext) : ILinkApi = {
    GetLinks =
        fun () ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    let! links = getLinks db userId |> Async.AwaitTask
                    return links |> Seq.map toSharedLink |> List.ofSeq
                }
    SaveLinks =
        fun links ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    do! saveLinks db userId links |> Async.AwaitTask
                }
}

let publicApiImplementation (ctx: HttpContext) : IPublicApi = {
    GetPreview =
        fun slug ->
            asyncResult {
                let db = ctx.GetService<AppDbContext>()
                let! profile, links = getPreview db slug |> Async.AwaitTask
                return (toSharedProfile profile, links |> Seq.map toSharedLink |> List.ofSeq)
            }
}

let linksApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi linksApiImplementation

let publicApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi publicApiImplementation
