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
        if existingUser = null then
            let logger = db.GetService<ILogger<Entity.AppDbContext>>()
            logger.LogInformation("Seeding development user '{Email}'", email)
            // Password is "secret123"
            let passwordHash = BCrypt.HashPassword("secret123")
            let devUser = new User(Id = 0, Email = email, PasswordHash = passwordHash)
            db.Users.Add(devUser) |> ignore
            let! _ = db.SaveChangesAsync()
            ()
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
        route "/" >=> text "OK"
        subRoute
            "/api/auth"
            (choose [
                POST >=> route "/register" >=> global.Auth.handleRegister
                POST >=> route "/login" >=> global.Auth.handleLogin
                POST >=> route "/logout" >=> global.Auth.handleLogout
            ])
        subRoute
            "/api"
            (global.Auth.requiresAuthentication
             >=> choose [
                 subRoute
                     "/profile"
                     (choose [
                         GET >=> route "" >=> global.Profile.handleGetProfile
                         PUT >=> route "" >=> global.Profile.handleSaveProfile
                     ])
                 subRoute
                     "/links"
                     (choose [
                         GET >=> route "" >=> global.Links.handleGetLinks
                         PUT >=> route "" >=> global.Links.handleSaveLinks
                     ])
             ])
        subRoute
            "/api/public"
            (choose [ GET >=> routef "/preview/%s" global.Links.handleGetPreview ])
    ]

// =====================
// App bootstrap
// =====================

[<EntryPoint>]
let main argv =
    let builder = WebApplication.CreateBuilder(argv)

    // Manually load the migrations assembly in dev/runtime containers.
    // Context: EF Core needs to load 'Backend.DbMigrations' at runtime to discover migrations.
    // In some container publish layouts, the DLL isn't included in the .deps.json binding context,
    // so we proactively load it here if the file exists. This is a pragmatic fix for dev/docker-compose.
    // Production best practice: run migrations via a separate Kubernetes Job; then this load
    // is not required because the app won't need to discover migrations at startup.
    let migAssemblyName = "Backend.DbMigrations"
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
            fun npgsqlOptions -> npgsqlOptions.MigrationsAssembly("Backend.DbMigrations") |> ignore
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
