module Auth

open System
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.EntityFrameworkCore
open Giraffe
open BCrypt.Net
open Entity

[<CLIMutable>]
type UserDto = { id: int; email: string }

[<CLIMutable>]
type RegisterUserRequest = { email: string; password: string }

[<CLIMutable>]
type LoginUserRequest = { email: string; password: string }

let requiresAuthentication: HttpHandler =
    fun next ctx ->
        let isAuthenticated =
            match ctx.User with
            | null -> false
            | user ->
                match user.Identity with
                | null -> false
                | identity -> identity.IsAuthenticated
        if isAuthenticated then
            next ctx
        else
            (setStatusCode 401 >=> text "User not authenticated.") next ctx

let getUserId (ctx: HttpContext) : int =
    match ctx.User with
    | null ->
        failwith
            "getUserId was called on an unauthenticated HttpContext. This indicates a programming error where an authenticated route did not use the 'requiresAuthentication' handler."
    | user ->
        match user.FindFirst "UserId" with
        | null ->
            failwith
                "UserId claim not found in token. This is unexpected for an authenticated user."
        | claim -> Int32.Parse claim.Value

let findUserByEmail (db: AppDbContext) (email: string) =
    db.Users.FirstOrDefaultAsync(fun u -> u.Email = email)

let handleRegister: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let! req = ctx.BindJsonAsync<RegisterUserRequest>()

            if
                req = null
                || String.IsNullOrWhiteSpace(req.email)
                || String.IsNullOrWhiteSpace(req.password)
            then
                return! (setStatusCode 400 >=> text "Email and password are required.") next ctx
            else
                let! existingUser = findUserByEmail db req.email
                if existingUser <> null then
                    return!
                        (setStatusCode 409 >=> text "A user with that email already exists.")
                            next
                            ctx
                else
                    let passwordHash = BCrypt.HashPassword(req.password)
                    let newUser = new User(Id = 0, Email = req.email, PasswordHash = passwordHash)
                    db.Users.Add(newUser) |> ignore
                    let! _ = db.SaveChangesAsync()

                    let claims = [
                        Claim(ClaimTypes.Name, newUser.Email)
                        Claim("UserId", newUser.Id.ToString())
                    ]
                    let identity =
                        ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
                    let principal = ClaimsPrincipal(identity)
                    do!
                        ctx.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            principal
                        )

                    let userDto: UserDto = { id = newUser.Id; email = newUser.Email }
                    return! (setStatusCode 201 >=> json userDto) next ctx
        }

let handleLogin: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let! req = ctx.BindJsonAsync<LoginUserRequest>()

            if
                req = null
                || String.IsNullOrWhiteSpace(req.email)
                || String.IsNullOrWhiteSpace(req.password)
            then
                return! (setStatusCode 401 >=> text "Invalid credentials.") next ctx
            else
                let! user = findUserByEmail db req.email
                match if isNull user then None else Some user with
                | None -> return! (setStatusCode 401 >=> text "Invalid credentials.") next ctx
                | Some user ->
                    if BCrypt.Verify(req.password, user.PasswordHash) then
                        let claims = [
                            Claim(ClaimTypes.Name, user.Email)
                            Claim("UserId", user.Id.ToString())
                        ]
                        let identity =
                            ClaimsIdentity(
                                claims,
                                CookieAuthenticationDefaults.AuthenticationScheme
                            )
                        let principal = ClaimsPrincipal(identity)
                        do!
                            ctx.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                principal
                            )

                        let userDto: UserDto = { id = user.Id; email = user.Email }
                        return! json userDto next ctx
                    else
                        return! (setStatusCode 401 >=> text "Invalid credentials.") next ctx
        }

let handleLogout: HttpHandler =
    fun next ctx ->
        task {
            do! ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            return! (setStatusCode 204) next ctx
        }
