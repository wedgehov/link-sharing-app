module Backend.Program

open System
open System.Linq
open System.ComponentModel.DataAnnotations.Schema
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Giraffe
open OpenTelemetry.Metrics
open OpenTelemetry.Trace
open Npgsql
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open System.Security.Claims
open BCrypt.Net
open Microsoft.EntityFrameworkCore.Infrastructure
open System.IO
open System.Runtime.Loader
open Entity
open Mapping
open System.Linq
open Shared.SharedModels
open System.Text
open System.Text.RegularExpressions
open System.Globalization

// =====================
// Helpers: Platform mapping between Entity and Shared
// =====================

// moved to Mapping.fs

// DTOs and auth handlers moved to Auth.fs

// =====================
// EF Core DbContext
// =====================

type AppDbContext = Entity.AppDbContext

// Profile slug helpers moved to Profile.fs

let seedDevelopmentData (db: Entity.AppDbContext) =
    task {
        // This function is idempotent. It won't fail if the user already exists.
        let email = "test@example.com"
        let! existingUser = db.Users.FirstOrDefaultAsync(fun u -> u.Email = email)
        let logger = db.GetService<ILogger<Entity.AppDbContext>>()
        match Option.ofObj existingUser with
        | None ->
            logger.LogInformation("Seeding development user '{Email}'", email)
            // Password is "secret123"
            let passwordHash = BCrypt.HashPassword("secret123")
            let devUser = User()
            devUser.Email <- email
            devUser.PasswordHash <- passwordHash
            devUser.PublicGuid <- Guid.NewGuid().ToString("D")
            devUser.Role <- UserRole.Admin
            db.Users.Add(devUser) |> ignore
            let! _ = db.SaveChangesAsync()
            ()
        | Some existingUser when existingUser.Role <> UserRole.Admin ->
            logger.LogInformation("Updating development user '{Email}' to admin role", email)
            existingUser.Role <- UserRole.Admin
            let! _ = db.SaveChangesAsync()
            ()
        | Some _ -> ()
    }

// Profile/Links data logic moved to Profile.fs and Links.fs

// =====================
// HTTP Handlers (Giraffe)
// =====================

// Authentication helpers moved to Auth.fs

// HTTP handlers moved into Profile.fs, Links.fs, and Auth.fs

// =====================
// Routes
// =====================

let webApp: HttpHandler =
    choose [
        global.Auth.authApiHandler
        routeStartsWith "/api/IProfileApi"
        >=> global.Auth.requiresAuthentication
        >=> global.Profile.profileApiHandler
        routeStartsWith "/api/ILinkApi"
        >=> global.Auth.requiresAuthentication
        >=> global.Links.linksApiHandler
        routef
            "/api/profile/%i/avatar"
            (fun userId ->
                POST
                >=> global.Auth.requiresAuthentication
                >=> global.AvatarUpload.uploadAvatarHandler userId
            )
        global.Links.publicApiHandler
        // Fallback for SPA routes so deep links resolve to index.html.
        fun next ctx ->
            let env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>()
            let webRoot =
                if String.IsNullOrEmpty(env.WebRootPath) then
                    Path.Combine(env.ContentRootPath, "wwwroot")
                else
                    env.WebRootPath

            let path = Path.Combine(webRoot, "index.html")

            if File.Exists(path) then
                htmlFile path next ctx
            else
                (setStatusCode 404 >=> text "Not Found") next ctx
    ]

// =====================
// App bootstrap
// =====================

[<EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder(argv)

    // Ensure the EF migrations assembly is loadable in all runtime environments.
    let migAssemblyName = "Entity"
    let migAssemblyPath =
        Path.Combine(AppContext.BaseDirectory, migAssemblyName + ".dll")
    if File.Exists migAssemblyPath then
        try
            AssemblyLoadContext.Default.LoadFromAssemblyPath(migAssemblyPath) |> ignore
        with _ ->
            ()

    // Configure logging (Serilog when enabled, else built-in)
    global.Logger.configure builder

    // Configure OpenTelemetry for metrics and tracing
    global.Observability.addOpenTelemetry builder.Services

    // Connection string must be provided:
    // - appsettings.json:  ConnectionStrings:DefaultConnection
    // - or env var:        DOTNET_ConnectionStrings__DefaultConnection
    let connStr = builder.Configuration.GetConnectionString("DefaultConnection")

    builder.Services.AddDbContext<AppDbContext>(fun (options: DbContextOptionsBuilder) ->
        options.UseNpgsql(
            connStr,
            fun npgsqlOptions -> npgsqlOptions.MigrationsAssembly("Entity") |> ignore
        )
        |> ignore
    )
    |> ignore

    // Add cookie authentication
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(fun options ->
            options.Cookie.Name <- "linksharing.auth"
            options.Cookie.HttpOnly <- true
            options.Cookie.SecurePolicy <- CookieSecurePolicy.SameAsRequest
            options.Cookie.SameSite <- SameSiteMode.Lax

            // On 401, don't redirect, just return the status code
            options.Events.OnRedirectToLogin <-
                (fun context ->
                    context.Response.StatusCode <- 401
                    Task.CompletedTask
                )
        )
    |> ignore

    // Add CORS to allow frontend requests
    builder.Services.AddCors(fun options ->
        options.AddDefaultPolicy(fun policy ->
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
            |> ignore
        )
        |> ignore
    )
    |> ignore

    builder.Services.AddGiraffe() |> ignore
    builder.Services.AddSingleton<
        global.AvatarStorage.IAvatarStorage,
        global.AvatarStorage.AzureBlobAvatarStorage
     >()
    |> ignore

    let app = builder.Build()

    // In a real app, you might use a scope to get the DbContext
    // For this simple startup task, getting it directly is fine.
    use scope = app.Services.CreateScope()
    let dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>()
    let logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>()

    // Retry logic for database initialization to handle startup race conditions in Docker.
    let maxRetries = 10
    let delay = TimeSpan.FromSeconds(5.0)
    let mutable retries = 0
    let mutable connected = false

    while not connected && retries < maxRetries do
        try
            // This applies any pending migrations to the database.
            dbContext.Database.Migrate()
            logger.LogInformation("Database migrations applied successfully")
            printfn "Database migrations applied successfully"
            connected <- true

            // Test the connection by checking if we can connect to the database
            let canConnect = dbContext.Database.CanConnect()
            if canConnect then
                logger.LogInformation("Database connection test successful")
                printfn "Database connection test successful"
            else
                logger.LogWarning("Database connection test failed")
                printfn "Database connection test failed"
        with ex ->
            retries <- retries + 1
            logger.LogError(
                ex,
                "Failed to initialize database. Attempt {RetryCount}/{MaxRetries}",
                retries,
                maxRetries
            )
            printfn
                "Failed to initialize database. Attempt %d/%d. Retrying in %f seconds..."
                retries
                maxRetries
                delay.TotalSeconds
            if retries < maxRetries then
                Task.Delay(delay).Wait()

    if not connected then
        let errorMsg = "Could not connect to the database after multiple retries. Exiting."
        logger.LogCritical(errorMsg)
        printfn "%s" errorMsg
        Environment.Exit(1)

    // Add the /metrics endpoint for Prometheus
    global.Observability.addPrometheusEndpoint app

    // Enable CORS
    app.UseCors() |> ignore

    app.UseAuthentication() |> ignore
    app.UseStaticFiles() |> ignore

    if app.Environment.IsDevelopment() then
        // Seed development-specific data on startup.
        use scope = app.Services.CreateScope()
        let dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>()
        seedDevelopmentData dbContext |> Async.AwaitTask |> Async.RunSynchronously

    // Global error handler then app
#if SERILOG
    app.UseSerilogRequestLogging() |> ignore
#endif
    app.UseGiraffeErrorHandler(global.ErrorHandling.globalErrorHandler) |> ignore
    app.UseGiraffe webApp |> ignore
    app.Run()
    0
