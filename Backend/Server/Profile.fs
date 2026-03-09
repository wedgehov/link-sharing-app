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

let saveProfile (db: AppDbContext) (userId: int) (profileDto: UserProfile) =
    taskResult {
        let! user =
            db.Users.FirstOrDefaultAsync(fun u -> u.Id = userId)
            |> TaskResult.ofTask
            |> TaskResult.map Option.ofObj
            |> TaskResult.bind (Result.requireSome Unauthorized >> TaskResult.ofResult)

        user.FirstName <- profileDto.FirstName
        user.LastName <- profileDto.LastName
        user.Email <-
            profileDto.DisplayEmail
            |> Option.filter (fun email -> not (String.IsNullOrWhiteSpace email))
            |> Option.defaultValue user.Email
        user.AvatarUrl <- normalizeOptionalText profileDto.AvatarUrl

        if String.IsNullOrWhiteSpace user.PublicGuid then
            user.PublicGuid <- Guid.NewGuid().ToString("D")

        do! db.SaveChangesAsync() |> TaskResult.ofTask |> TaskResult.ignore
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

let profileApiImplementation (ctx: HttpContext) : IProfileApi = {
    GetProfile =
        fun () ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    let! profile = getProfile db userId |> Async.AwaitTask
                    return profile |> Option.map toSharedProfile |> Option.defaultValue emptyProfile
                }
    SaveProfile =
        fun profileDto ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    do! saveProfile db userId profileDto |> Async.AwaitTask
                }
}

let profileApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi profileApiImplementation
