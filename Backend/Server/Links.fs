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
            db.UserProfiles.FirstOrDefaultAsync(fun (p: Entity.UserProfile) -> p.UserId = userId)

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
            db.Links.Where(fun (l: Entity.Link) -> l.UserProfileId = userProfile.Id).ToListAsync()

        db.Links.RemoveRange(existingLinks) |> ignore

        for linkDto in linksDto do
            let newLink = Entity.Link()
            newLink.Url <- linkDto.Url
            newLink.Platform <- toEntityPlatform linkDto.Platform
            newLink.SortOrder <- linkDto.SortOrder
            newLink.UserProfileId <- userProfile.Id
            db.Links.Add(newLink) |> ignore

        let! _ = db.SaveChangesAsync()
        return ()
    }

let getPreview (db: AppDbContext) (slug: string) =
    task {
        let! profile = db.UserProfiles.FirstOrDefaultAsync(fun p -> p.ProfileSlug = slug)

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
                    let! links = getLinks db userId |> Async.AwaitTask |> AsyncResult.ofAsync
                    return links |> Seq.map toSharedLink |> List.ofSeq
                }
    SaveLinks =
        fun links ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    do!
                        saveLinks db userId links
                        |> Async.AwaitTask
                        |> AsyncResult.ofAsync
                        |> AsyncResult.ignore
                }
}

let publicApiImplementation (ctx: HttpContext) : IPublicApi = {
    GetPreview =
        fun slug ->
            asyncResult {
                let db = ctx.GetService<AppDbContext>()
                let! preview = getPreview db slug |> Async.AwaitTask |> AsyncResult.ofAsync
                let! found = preview |> Result.requireSome NotFound |> AsyncResult.ofResult
                let profile, links = found
                return (toSharedProfile profile, links |> Seq.map toSharedLink |> List.ofSeq)
            }
}

let linksApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi linksApiImplementation

let publicApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi publicApiImplementation
