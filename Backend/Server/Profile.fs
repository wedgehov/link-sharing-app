module Profile

open System
open System.Text
open System.Text.RegularExpressions
open System.Globalization
open FsToolkit.ErrorHandling
open Entity
open Giraffe
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.Http
open Shared.SharedModels
open Shared.Api

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

let private toSharedProfile (p: Entity.UserProfile) : UserProfile = {
    FirstName = if isNull p.FirstName then "" else p.FirstName
    LastName = if isNull p.LastName then "" else p.LastName
    DisplayEmail =
        if isNull p.DisplayEmail || String.IsNullOrWhiteSpace p.DisplayEmail then
            None
        else
            Some p.DisplayEmail
    ProfileSlug = if isNull p.ProfileSlug then "" else p.ProfileSlug
    AvatarUrl =
        if isNull p.AvatarUrl || String.IsNullOrWhiteSpace p.AvatarUrl then
            None
        else
            Some p.AvatarUrl
}

let private emptyProfile: UserProfile = {
    FirstName = ""
    LastName = ""
    DisplayEmail = None
    ProfileSlug = ""
    AvatarUrl = None
}

let profileApiImplementation (ctx: HttpContext) : IProfileApi = {
    GetProfile =
        fun () ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    let! profile = getProfile db userId |> Async.AwaitTask |> AsyncResult.ofAsync
                    return profile |> Option.map toSharedProfile |> Option.defaultValue emptyProfile
                }
    SaveProfile =
        fun profileDto ->
            Auth.requireUser ctx
            <| fun userId ->
                asyncResult {
                    let db = ctx.GetService<AppDbContext>()
                    do!
                        saveProfile db userId profileDto
                        |> Async.AwaitTask
                        |> AsyncResult.ofAsync
                        |> AsyncResult.ignore
                }
}

let profileApiHandler: HttpHandler =
    RemotingUtil.handlerFromApi profileApiImplementation
