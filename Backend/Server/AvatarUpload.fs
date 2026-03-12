module AvatarUpload

open System
open System.IO
open Entity
open Microsoft.AspNetCore.Http
open Microsoft.EntityFrameworkCore
open Giraffe
open Shared.Api
open AvatarStorage
open FsToolkit.ErrorHandling

let private isAllowedContentType (ct: string) =
    let v = ct.ToLowerInvariant()
    v = "image/jpeg" || v = "image/png"

let private getExtension (ct: string) =
    let v = ct.ToLowerInvariant()
    if v = "image/jpeg" then ".jpg"
    elif v = "image/png" then ".png"
    else ""

let uploadAvatarHandler (userId: int) : HttpHandler =
    fun next ctx ->
        task {
            // Reuse your existing auth rule (admins can upload for viewed user)
            let! result =
                Auth.requireAuthorization
                    ctx
                    userId
                    (fun authorizedUserId ->
                        asyncResult {
                            do!
                                ctx.Request.HasFormContentType
                                |> Result.requireTrue (
                                    ValidationError "Expected multipart/form-data"
                                )

                            let! form = ctx.Request.ReadFormAsync() |> Async.AwaitTask

                            let! file =
                                form.Files.GetFile("avatar")
                                |> Option.ofObj
                                |> Result.requireSome (ValidationError "Missing avatar file")

                            do!
                                (file.Length > 0L)
                                |> Result.requireTrue (ValidationError "Missing avatar file")

                            do!
                                (file.Length <= 5L * 1024L * 1024L) // 5MB limit
                                |> Result.requireTrue (ValidationError "Avatar must be <= 5MB")

                            do!
                                isAllowedContentType file.ContentType
                                |> Result.requireTrue (ValidationError "Only PNG/JPEG is allowed")

                            let storage = ctx.GetService<IAvatarStorage>()
                            let db = ctx.GetService<AppDbContext>()
                            let! maybeUser =
                                db.Users.FirstOrDefaultAsync(fun u -> u.Id = authorizedUserId)
                                |> Async.AwaitTask
                                |> AsyncResult.ofAsync
                            let! user =
                                maybeUser
                                |> Option.ofObj
                                |> Result.requireSome Unauthorized
                                |> AsyncResult.ofResult

                            let oldAvatarUrl = user.AvatarUrl |> Option.ofObj
                            use stream = file.OpenReadStream()
                            let extension = getExtension file.ContentType

                            let! uploadedUrl =
                                storage.UploadAvatarAsync(
                                    authorizedUserId,
                                    stream,
                                    file.ContentType,
                                    extension
                                )
                                |> Async.AwaitTask

                            user.AvatarUrl <- uploadedUrl
                            do!
                                db.SaveChangesAsync()
                                |> Async.AwaitTask
                                |> AsyncResult.ofAsync
                                |> AsyncResult.ignore

                            match oldAvatarUrl with
                            | Some old when old <> uploadedUrl ->
                                do!
                                    storage.DeleteAvatarIfOwnedAsync(old)
                                    |> Async.AwaitTask
                                    |> AsyncResult.ofAsync
                            | _ -> ()

                            return uploadedUrl
                        }
                    )

            match result with
            | Ok url -> return! json {| url = url |} next ctx
            | Error(ValidationError msg) ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Error Unauthorized -> return! setStatusCode 401 next ctx
            | Error _ -> return! (setStatusCode 500 >=> json {| error = "Upload failed" |}) next ctx
        }
