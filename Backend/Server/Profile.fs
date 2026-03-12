module Profile

open System
open FsToolkit.ErrorHandling
open Entity
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Shared.SharedModels
open Shared.Api

let private normalizeOptionalText (value: string option) =
    value
    |> Option.filter (fun v -> not (String.IsNullOrWhiteSpace v))
    |> Option.toObj

let private textOrEmpty (value: string | null) = value |> Option.ofObj |> Option.defaultValue ""

let private nonBlankOption (value: string | null) =
    value
    |> Option.ofObj
    |> Option.filter (fun v -> not (String.IsNullOrWhiteSpace v))

let getProfile (db: AppDbContext) (userId: int) =
    db.Users.AsNoTracking().FirstOrDefaultAsync(fun u -> u.Id = userId)
    |> TaskResult.ofTask
    |> TaskResult.map Option.ofObj

let saveProfile
    (db: AppDbContext)
    (storage: AvatarStorage.IAvatarStorage option)
    (userId: int)
    (profileDto: UserProfile)
    =
    taskResult {
        let! user =
            db.Users.FirstOrDefaultAsync(fun u -> u.Id = userId)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj
            |> TaskResult.bind (Result.requireSome Unauthorized >> TaskResult.ofResult)

        user.FirstName <- profileDto.FirstName
        user.LastName <- profileDto.LastName
        let requestedEmail =
            profileDto.DisplayEmail
            |> Option.map (fun email -> email.Trim())
            |> Option.filter (fun email -> not (String.IsNullOrWhiteSpace email))
            |> Option.defaultValue user.Email

        let! emailInUseByAnotherUser =
            db.Users.AsNoTracking().AnyAsync(fun u -> u.Id <> userId && u.Email = requestedEmail)
            |> TaskResult.ofTask

        do! emailInUseByAnotherUser |> Result.requireFalse Conflict |> TaskResult.ofResult

        user.Email <- requestedEmail

        let oldAvatarUrl = if isNull user.AvatarUrl then "" else user.AvatarUrl
        let newAvatarUrl = normalizeOptionalText profileDto.AvatarUrl
        user.AvatarUrl <- newAvatarUrl

        if String.IsNullOrWhiteSpace user.PublicGuid then
            user.PublicGuid <- Guid.NewGuid().ToString("D")

        do! db.SaveChangesAsync() |> TaskResult.ofTask |> TaskResult.ignore

        match storage with
        | Some s when
            oldAvatarUrl <> (if isNull newAvatarUrl then "" else newAvatarUrl)
            && not (String.IsNullOrWhiteSpace oldAvatarUrl)
            ->
            // Best effort delete in background
            s.DeleteAvatarIfOwnedAsync(oldAvatarUrl) |> ignore
        | _ -> ()
    }

let private toSharedProfile (u: Entity.User) : UserProfile = {
    FirstName = textOrEmpty u.FirstName
    LastName = textOrEmpty u.LastName
    DisplayEmail = Some u.Email
    AvatarUrl = nonBlankOption u.AvatarUrl
}

let private emptyProfile: UserProfile = {
    FirstName = ""
    LastName = ""
    DisplayEmail = None
    AvatarUrl = None
}

let private getProfileByUserId (ctx: HttpContext) (userId: int) =
    asyncResult {
        let db = ctx.GetService<AppDbContext>()
        let! profile = getProfile db userId |> Async.AwaitTask
        return profile |> Option.map toSharedProfile |> Option.defaultValue emptyProfile
    }

let private saveProfileByUserId (ctx: HttpContext) (userId: int) (profileDto: UserProfile) =
    asyncResult {
        let db = ctx.GetService<AppDbContext>()
        let storage = ctx.GetService<AvatarStorage.IAvatarStorage>()
        do! saveProfile db (Some storage) userId profileDto |> Async.AwaitTask
    }

let profileApiImplementation (ctx: HttpContext) : IProfileApi = {
    GetProfile =
        fun userId ->
            Auth.requireAuthorization ctx userId
            <| fun authorizedUserId -> getProfileByUserId ctx authorizedUserId
    SaveProfile =
        fun userId profileDto ->
            Auth.requireAuthorization ctx userId
            <| fun authorizedUserId -> saveProfileByUserId ctx authorizedUserId profileDto
}

let profileApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi profileApiImplementation
