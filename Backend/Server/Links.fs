module Links

open System
open FsToolkit.ErrorHandling
open System.Linq
open Entity
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Shared.SharedModels
open Shared.Api
open Mapping

let private normalizePublicId (value: string) =
    match Guid.TryParse value with
    | true, guid -> Some(guid.ToString("D"))
    | _ -> None

let private textOrEmpty (value: string | null) = value |> Option.ofObj |> Option.defaultValue ""

let private nonBlankOption (value: string | null) =
    value
    |> Option.ofObj
    |> Option.filter (fun v -> not (String.IsNullOrWhiteSpace v))

let getLinks (db: AppDbContext) (userId: int) =
    db.Links
        .AsQueryable()
        .Where(fun (l: Entity.Link) -> l.UserId = userId)
        .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
        .ToListAsync()
    |> TaskResult.ofTask

let saveLinks (db: AppDbContext) (userId: int) (linksDto: Link list) =
    taskResult {
        let! existingLinks =
            db.Links.Where(fun (l: Entity.Link) -> l.UserId = userId).ToListAsync()
            |> TaskResult.ofTask

        db.Links.RemoveRange(existingLinks) |> ignore

        for linkDto in linksDto do
            let newLink = Entity.Link()
            newLink.Url <- linkDto.Url
            newLink.Platform <- toEntityPlatform linkDto.Platform
            newLink.SortOrder <- linkDto.SortOrder
            newLink.UserId <- userId
            db.Links.Add(newLink) |> ignore

        do! db.SaveChangesAsync() |> TaskResult.ofTask |> TaskResult.ignore
    }

let getPreview (db: AppDbContext) (publicId: string) =
    taskResult {
        let! normalizedPublicId =
            publicId
            |> normalizePublicId
            |> Result.requireSome NotFound
            |> TaskResult.ofResult

        let! user =
            db.Users.AsNoTracking().FirstOrDefaultAsync(fun u -> u.PublicGuid = normalizedPublicId)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj
            |> TaskResult.bind (Result.requireSome NotFound >> TaskResult.ofResult)

        let! links =
            db.Links
                .Where(fun (l: Entity.Link) -> l.UserId = user.Id)
                .OrderBy(fun (l: Entity.Link) -> l.SortOrder)
                .ToListAsync()
            |> TaskResult.ofTask

        return user, links
    }

let private toSharedLink (link: Entity.Link) : Link = {
    Id = Some link.Id
    Platform = toSharedPlatform link.Platform
    Url = textOrEmpty link.Url
    SortOrder = link.SortOrder
}

let private toSharedProfile (user: Entity.User) : UserProfile = {
    FirstName = textOrEmpty user.FirstName
    LastName = textOrEmpty user.LastName
    DisplayEmail = Some user.Email
    AvatarUrl = nonBlankOption user.AvatarUrl
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
        fun publicId ->
            asyncResult {
                let db = ctx.GetService<AppDbContext>()
                let! user, links = getPreview db publicId |> Async.AwaitTask
                return (toSharedProfile user, links |> Seq.map toSharedLink |> List.ofSeq)
            }
}

let linksApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi linksApiImplementation

let publicApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi publicApiImplementation
