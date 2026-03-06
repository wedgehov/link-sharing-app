module Profile

open System
open System.Text
open System.Text.RegularExpressions
open System.Globalization
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Giraffe
open Entity
open Shared.SharedModels
open Dtos

let private removeDiacritics (input: string) : string =
    if String.IsNullOrWhiteSpace input then
        ""
    else
        let normalized = input.Normalize(NormalizationForm.FormD)
        let sb = StringBuilder()
        for ch in normalized do
            let uc = CharUnicodeInfo.GetUnicodeCategory(ch)
            if uc <> UnicodeCategory.NonSpacingMark then
                sb.Append(ch) |> ignore
        sb.ToString().Normalize(NormalizationForm.FormC)

let private slugify (input: string) : string =
    let ascii = removeDiacritics input
    let lower = ascii.ToLowerInvariant()
    let replaced = Regex.Replace(lower, "[^a-z0-9]+", "-")
    Regex.Replace(replaced, "(^-+|-+$)", "")

let private ensureUniqueSlug (db: AppDbContext) (baseSlug: string) =
    task {
        let base' =
            if String.IsNullOrWhiteSpace baseSlug then
                "user"
            else
                baseSlug
        let mutable candidate = base'
        let mutable n = 1
        let mutable exists = true
        while exists do
            let! found = db.UserProfiles.AnyAsync(fun p -> p.ProfileSlug = candidate)
            if found then
                n <- n + 1
                candidate <- sprintf "%s-%d" base' n
            else
                exists <- false
        return candidate
    }

let private generateInitialSlug
    (firstName: string)
    (lastName: string)
    (displayEmail: string option)
    (userId: int)
    : string =
    let fromNames =
        let full =
            String
                .Join(
                    " ",
                    [|
                        defaultArg (if isNull firstName then None else Some firstName) ""
                        defaultArg (if isNull lastName then None else Some lastName) ""
                    |]
                )
                .Trim()
        if String.IsNullOrWhiteSpace full then
            None
        else
            Some(slugify full)
    match fromNames with
    | Some s when s <> "" -> s
    | _ ->
        match displayEmail with
        | Some em when not (String.IsNullOrWhiteSpace em) ->
            let local = em.Split('@').[0]
            let s = slugify local
            if String.IsNullOrWhiteSpace s then
                sprintf "user-%d" userId
            else
                s
        | _ -> sprintf "user-%d" userId

let getProfile (db: AppDbContext) (userId: int) =
    task {
        let! profile = db.UserProfiles.FirstOrDefaultAsync(fun p -> p.UserId = userId)
        return if isNull profile then None else Some profile
    }

let saveProfile (db: AppDbContext) (userId: int) (profileDto: UserProfile) =
    task {
        let! profile =
            db.UserProfiles.FirstOrDefaultAsync(fun (p: Entity.UserProfile) -> p.UserId = userId)
        match if isNull profile then None else Some profile with
        | None ->
            let baseSlug =
                if String.IsNullOrWhiteSpace profileDto.ProfileSlug then
                    generateInitialSlug
                        profileDto.FirstName
                        profileDto.LastName
                        profileDto.DisplayEmail
                        userId
                else
                    slugify profileDto.ProfileSlug
            let! uniqueSlug = ensureUniqueSlug db baseSlug
            let newProfile = Entity.UserProfile()
            newProfile.FirstName <- profileDto.FirstName
            newProfile.LastName <- profileDto.LastName
            newProfile.DisplayEmail <-
                (match profileDto.DisplayEmail with
                 | Some v -> v
                 | None -> null)
            newProfile.ProfileSlug <- uniqueSlug
            newProfile.AvatarUrl <-
                (match profileDto.AvatarUrl with
                 | Some v -> v
                 | None -> null)
            newProfile.UserId <- userId
            db.UserProfiles.Add(newProfile) |> ignore
        | Some(p: Entity.UserProfile) ->
            p.FirstName <- profileDto.FirstName
            p.LastName <- profileDto.LastName
            p.DisplayEmail <-
                (match profileDto.DisplayEmail with
                 | Some v -> v
                 | None -> null)
            if String.IsNullOrWhiteSpace p.ProfileSlug then
                let baseSlug =
                    if String.IsNullOrWhiteSpace profileDto.ProfileSlug then
                        generateInitialSlug
                            profileDto.FirstName
                            profileDto.LastName
                            profileDto.DisplayEmail
                            userId
                    else
                        slugify profileDto.ProfileSlug
                let! uniqueSlug = ensureUniqueSlug db baseSlug
                p.ProfileSlug <- uniqueSlug
            p.AvatarUrl <-
                (match profileDto.AvatarUrl with
                 | Some v -> v
                 | None -> null)

        let! _ = db.SaveChangesAsync()
        return ()
    }

let handleGetProfile: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let userId = Auth.getUserId ctx
            let! profile = getProfile db userId
            match profile with
            | Some p ->
                let profileDto: ProfileOutputDto = {
                    firstName = if isNull p.FirstName then "" else p.FirstName
                    lastName = if isNull p.LastName then "" else p.LastName
                    displayEmail = if isNull p.DisplayEmail then "" else p.DisplayEmail
                    profileSlug = p.ProfileSlug
                    avatarUrl = if isNull p.AvatarUrl then "" else p.AvatarUrl
                }
                return! json profileDto next ctx
            | None ->
                // Return an empty/default profile rather than 404 to simplify first-run UX.
                let dto: ProfileOutputDto = {
                    firstName = ""
                    lastName = ""
                    displayEmail = ""
                    profileSlug = ""
                    avatarUrl = ""
                }
                return! json dto next ctx
        }

let handleSaveProfile: HttpHandler =
    fun next ctx ->
        task {
            let db: AppDbContext = ctx.GetService<AppDbContext>()
            let userId = Auth.getUserId ctx
            let! input = ctx.BindJsonAsync<ProfileInputDto>()
            // Map DTO (strings) to shared model (options)
            let dto: UserProfile = {
                FirstName = input.firstName
                LastName = input.lastName
                DisplayEmail =
                    if System.String.IsNullOrWhiteSpace input.displayEmail then
                        None
                    else
                        Some input.displayEmail
                ProfileSlug = input.profileSlug
                AvatarUrl =
                    if System.String.IsNullOrWhiteSpace input.avatarUrl then
                        None
                    else
                        Some input.avatarUrl
            }
            do! saveProfile db userId dto
            return! (setStatusCode 204 >=> text "") next ctx
        }
