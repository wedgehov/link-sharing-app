module Auth

open System
open System.Security.Claims
open BCrypt.Net
open FsToolkit.ErrorHandling
open Giraffe
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Shared.Api

let private hasAuthenticatedIdentity (user: ClaimsPrincipal) =
    user.Identity
    |> Option.ofObj
    |> Option.exists (fun identity -> identity.IsAuthenticated)

let requiresAuthentication: HttpHandler =
    fun next ctx ->
        if hasAuthenticatedIdentity ctx.User then
            next ctx
        else
            (setStatusCode 401 >=> text "User not authenticated.") next ctx

let private tryParseInt (value: string) =
    match Int32.TryParse value with
    | true, parsed -> Some parsed
    | false, _ -> None

let requireUser
    (ctx: HttpContext)
    (operation: int -> Async<Result<'ok, AppError>>)
    : Async<Result<'ok, AppError>> =
    asyncResult {
        let! userId =
            ctx.User.FindFirst "UserId"
            |> Option.ofObj
            |> Option.bind (fun claim -> tryParseInt claim.Value)
            |> Result.requireSome Unauthorized
            |> AsyncResult.ofResult

        return! operation userId
    }

let requireAuthorization
    (ctx: HttpContext)
    (resourceOwnerId: int)
    (operation: int -> Async<Result<'ok, AppError>>)
    : Async<Result<'ok, AppError>> =
    requireUser ctx
    <| fun userId ->
        asyncResult {
            do!
                userId = resourceOwnerId
                |> Result.requireTrue Unauthorized
                |> AsyncResult.ofResult

            return! operation userId
        }

let private toSharedUser (user: Entity.User) : User = {
    Id = user.Id
    Email = user.Email
    PublicId =
        if String.IsNullOrWhiteSpace user.PublicGuid then
            ""
        else
            user.PublicGuid
}

let private createPrincipal (email: string) (userId: int) =
    let claims = [ Claim(ClaimTypes.Name, email); Claim("UserId", userId.ToString()) ]
    let identity =
        ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
    ClaimsPrincipal(identity)

let private signInUser (ctx: HttpContext) (user: Entity.User) =
    let principal = createPrincipal user.Email user.Id
    ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)
    |> Async.AwaitTask
    |> AsyncResult.ofAsync

let private register (ctx: HttpContext) (db: Entity.AppDbContext) (req: RegisterRequest) =
    asyncResult {
        do!
            (String.IsNullOrWhiteSpace(req.Email) || String.IsNullOrWhiteSpace(req.Password))
            |> Result.requireFalse (ValidationError "Email and password are required.")
            |> AsyncResult.ofResult

        do!
            db.Users.AsNoTracking().FirstOrDefaultAsync(fun u -> u.Email = req.Email)
            |> Async.AwaitTask
            |> AsyncResult.ofAsync
            |> AsyncResult.map Option.ofObj
            |> AsyncResult.bindRequireNone Conflict

        let passwordHash = BCrypt.HashPassword(req.Password)
        let newUser = Entity.User()
        newUser.Email <- req.Email
        newUser.PasswordHash <- passwordHash
        db.Users.Add(newUser) |> ignore

        do!
            db.SaveChangesAsync()
            |> Async.AwaitTask
            |> AsyncResult.ofAsync
            |> AsyncResult.ignore

        do! signInUser ctx newUser
        return toSharedUser newUser
    }

let private login (ctx: HttpContext) (db: Entity.AppDbContext) (req: LoginRequest) =
    asyncResult {
        do!
            (String.IsNullOrWhiteSpace(req.Email) || String.IsNullOrWhiteSpace(req.Password))
            |> Result.requireFalse InvalidCredentials
            |> AsyncResult.ofResult

        let! user =
            db.Users.AsNoTracking().FirstOrDefaultAsync(fun u -> u.Email = req.Email)
            |> Async.AwaitTask
            |> AsyncResult.ofAsync
            |> AsyncResult.map Option.ofObj
            |> AsyncResult.bindRequireSome InvalidCredentials

        do!
            BCrypt.Verify(req.Password, user.PasswordHash)
            |> Result.requireTrue InvalidCredentials
            |> AsyncResult.ofResult

        do! signInUser ctx user
        return toSharedUser user
    }

let private logout (ctx: HttpContext) () =
    asyncResult {
        do!
            ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            |> Async.AwaitTask
            |> AsyncResult.ofAsync
    }

let private getCurrentUser (ctx: HttpContext) () =
    requireUser ctx
    <| fun userId ->
        asyncResult {
            let db = ctx.GetService<Entity.AppDbContext>()
            let! user =
                db.Users.AsNoTracking().FirstOrDefaultAsync(fun u -> u.Id = userId)
                |> Async.AwaitTask
                |> AsyncResult.ofAsync
                |> AsyncResult.map Option.ofObj
                |> AsyncResult.bindRequireSome Unauthorized

            return toSharedUser user
        }

let authApiImplementation (ctx: HttpContext) : IAuthApi =
    let db = ctx.GetService<Entity.AppDbContext>()
    {
        Register = register ctx db
        Login = login ctx db
        Logout = logout ctx
        GetCurrentUser = getCurrentUser ctx
    }

let authApiHandler: HttpHandler = RemotingUtil.handlerFromApi authApiImplementation
