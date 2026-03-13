module AvatarStorage

open System
open System.IO
open System.Threading.Tasks
open Azure.Identity
open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Shared.Api

type IAvatarStorage =
    abstract member UploadAvatarAsync:
        userId: int * stream: Stream * contentType: string * extension: string ->
            Task<Result<string, AppError>>
    abstract member DeleteAvatarIfOwnedAsync: oldUrl: string -> Task<unit>

type AzureBlobAvatarStorage(configuration: IConfiguration, logger: ILogger<AzureBlobAvatarStorage>)
    =

    let containerName =
        configuration.GetValue<string>("BlobStorage:ContainerName", "avatars")

    let containerClient =
        let accountUrl = configuration.GetValue<string>("BlobStorage:AccountUrl")
        let connectionString =
            configuration.GetValue<string>("BlobStorage:ConnectionString")

        if not (String.IsNullOrWhiteSpace(accountUrl)) then
            let safeAccountUrl = Option.ofObj accountUrl |> Option.defaultValue ""
            logger.LogInformation(
                "Using DefaultAzureCredential for Azure Blob Storage at {AccountUrl}",
                safeAccountUrl
            )
            BlobContainerClient(
                Uri(safeAccountUrl.TrimEnd('/') + "/" + containerName),
                DefaultAzureCredential()
            )
        elif not (String.IsNullOrWhiteSpace(connectionString)) then
            logger.LogInformation("Using connection string for Azure Blob Storage")
            BlobContainerClient(connectionString, containerName)
        else
            failwith
                "Neither BlobStorage:AccountUrl nor BlobStorage:ConnectionString is configured."

    // Ensure container exists on startup (best effort)
    do
        try
            containerClient.CreateIfNotExists(PublicAccessType.Blob) |> ignore
        with ex ->
            logger.LogWarning(
                ex,
                "Could not ensure container '{ContainerName}' exists. It might already exist or permissions might be restricted.",
                containerName
            )

    interface IAvatarStorage with
        member this.UploadAvatarAsync
            (userId: int, stream: Stream, contentType: string, extension: string)
            =
            task {
                try
                    let blobName = $"user-{userId}-{Guid.NewGuid():N}{extension}"
                    let blobClient = containerClient.GetBlobClient(blobName)

                    let headers = BlobHttpHeaders(ContentType = contentType)
                    let! _ = blobClient.UploadAsync(stream, headers)

                    let mutable returnUrl = blobClient.Uri.ToString()
                    // Workaround for local development using docker-compose: the browser needs to reach localhost, not the docker network alias "azurite"
                    if returnUrl.Contains("://azurite:10000") then
                        returnUrl <- returnUrl.Replace("://azurite:10000", "://localhost:10000")

                    return Ok(returnUrl)
                with ex ->
                    logger.LogError(ex, "Failed to upload avatar for user {UserId}", userId)
                    return Error(Unexpected "Failed to upload avatar to storage")
            }

        member this.DeleteAvatarIfOwnedAsync (oldUrl: string) =
            task {
                if String.IsNullOrWhiteSpace(oldUrl) then
                    return ()

                try
                    // Basic safety check to ensure we only delete blobs from our container
                    let mutable urlToCheck = oldUrl
                    // Translate local dev URL back to container network URL if needed
                    if urlToCheck.Contains("://localhost:10000") then
                        urlToCheck <- urlToCheck.Replace("://localhost:10000", "://azurite:10000")
                    elif urlToCheck.Contains("://127.0.0.1:10000") then
                        urlToCheck <- urlToCheck.Replace("://127.0.0.1:10000", "://azurite:10000")

                    if
                        urlToCheck.StartsWith(
                            containerClient.Uri.ToString(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    then
                        let uri = Uri(urlToCheck)
                        let blobName = uri.Segments.[uri.Segments.Length - 1]
                        let blobClient = containerClient.GetBlobClient(blobName)
                        let! _ = blobClient.DeleteIfExistsAsync()
                        ()
                with ex ->
                    logger.LogWarning(ex, "Failed to delete old avatar at {OldUrl}", oldUrl)
            }
