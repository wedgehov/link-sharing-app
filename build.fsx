#r "nuget: Fun.Build, 1.1.2"
#r "nuget: Fake.IO.FileSystem, 6.0.0"

open System.IO
open Fake.IO
open Fake.IO.FileSystemOperators
open Fun.Build

let deployDir = Path.getFullName "dist"

let restoreStage =
    stage "Restore" {
        run "dotnet tool restore"
        run "dotnet restore"

        stage "BunInstall" {
            workingDir "Client"
            run "bun i"
        }
    }

let clean (input: string seq) =
    async {
        input
        |> Seq.iter (fun dir ->
            if Directory.Exists(dir) then
                Directory.Delete(dir, true))
    }

pipeline "Bundle" {
    workingDir __SOURCE_DIRECTORY__
    restoreStage
    stage "Clean" { run (clean [| "dist"; "Client/dist"; "Client/.build" |]) }

    stage "Main" {
        paralle

        stage "Client" {
            workingDir "Client"
            // Compile F# first, then bundle with Vite (CI-friendly, no vite-plugin-fable daemon).
            run "dotnet fable src/src.fsproj -s -o .build --run bun run build"
        }

        stage "Server" {
            workingDir "Backend/Server"
            run $"dotnet publish -c Release -o %s{deployDir} -tl"
        }
    }

    // After parallel build finishes, copy the frontend to backend's wwwroot
    stage "PostBuild" {
        run $"mkdir -p %s{deployDir}/wwwroot"
        run $"cp -R Client/dist/ %s{deployDir}/wwwroot/"
    }

    runIfOnlySpecified false
}

pipeline "Watch" {
    workingDir __SOURCE_DIRECTORY__
    stage "Clean" { run (clean [| "dist"; "Client/dist"; "Client/.build" |]) }
    restoreStage

    stage "Main" {
        paralle

        stage "Client" {
            workingDir "Client"
            // Keep local dev aligned with production compilation path.
            run "dotnet fable watch src/src.fsproj -s -o .build --run bun run dev"
        }

        stage "Server" {
            workingDir "Backend/Server"
            envVars [ "ASPNETCORE_ENVIRONMENT", "Development" ]
            run "dotnet watch run -tl"
        }
    }

    runIfOnlySpecified true
}

pipeline "Format" {
    workingDir __SOURCE_DIRECTORY__
    stage "Restore" { run "dotnet tool restore" }
    stage "Fantomas" { run "dotnet fantomas . --exclude Client/node_modules" }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
