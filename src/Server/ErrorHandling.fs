module ErrorHandling

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Giraffe

/// Global error handler for Giraffe that logs unexpected exceptions and returns a standard 500 response.
let globalErrorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "Unhandled exception")
    clearResponse
    >=> setStatusCode StatusCodes.Status500InternalServerError
    >=> text "An unexpected error occurred. Please try again later."
