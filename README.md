# Frontend Mentor - Link-sharing app solution

This is a full-stack solution to the [Link-sharing app challenge on Frontend Mentor](https://www.frontendmentor.io/challenges/linksharing-app-Fbt7yweGsT). Frontend Mentor challenges help you improve your coding skills by building realistic projects.

## Table of contents

- [Overview](#overview)
  - [The challenge](#the-challenge)
  - [Links](#links)
- [My process](#my-process)
  - [Built with](#built-with)
  - [What I learned](#what-i-learned)
  - [Continued development](#continued-development)
- [Getting started](#getting-started)
- [Database migrations](#database-migrations)
- [Deployment](#deployment)
- [Author](#author)

## Overview

### The challenge

Users should be able to:

- Create, read, update, delete links and see previews in the mobile mockup
- Receive validations if the links form is submitted without a URL or with the wrong URL pattern for the platform
- Drag and drop links to reorder them
- Add profile details like profile picture, first name, last name, and email
- Receive validations if the profile details form is saved with no first or last name
- Preview their devlinks profile and copy the link to their clipboard
- View the optimal layout for the interface depending on their device's screen size
- See hover and focus states for all interactive elements on the page
- **Bonus**: Save details to a database (full-stack app)
- **Bonus**: Create an account and log in (user authentication)
- **Bonus**: Role-based authorization with admin users who can view and edit any user's pages
- **Bonus**: Profile images are uploaded to Azure Blob Storage via a dedicated multipart endpoint, with old image cleanup and fallback to local Azurite for development.

### Links

- Solution URL: [https://github.com/wedgehov/link-sharing-app](https://github.com/wedgehov/link-sharing-app)
- Live Site URL: [https://link-sharing-app-main.vhovet.com](https://link-sharing-app-main.vhovet.com)

## My process

### Built with

**Frontend:**

- [F#](https://fsharp.org/) with [Fable](https://fable.io/) compiled to JavaScript
- [Elmish](https://elmish.github.io/elmish/) for state management (The Elm Architecture)
- [Feliz](https://zaid-ajaj.github.io/Feliz/) for a type-safe React DSL
- [React 19](https://react.dev/)
- [Vite 7](https://vitejs.dev/)
- [Tailwind CSS 4](https://tailwindcss.com/) with custom design tokens

**Backend:**

- [F#](https://fsharp.org/) with [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) (.NET 10)
- [Giraffe](https://github.com/giraffe-fsharp/Giraffe) as a functional web framework
- [Fable.Remoting](https://zaid-ajaj.github.io/Fable.Remoting/) for typed RPC between client and server
- [FsToolkit.ErrorHandling](https://github.com/demystifyfp/FsToolkit.ErrorHandling) for composable error workflows
- [Azure.Storage.Blobs](https://learn.microsoft.com/en-us/azure/storage/blobs/) for profile image storage
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) with PostgreSQL (Code-First)
- Cookie-based authentication with role claims and [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) for password hashing

**DevOps:**

- Docker & Docker Compose (for infrastructure)
- Fun.Build (F# Make) for local build orchestration
- Nix for reproducible development environments
- GitHub Actions for CI (builds and pushes Docker image to GHCR)
- ArgoCD + Helm for GitOps deployment to Kubernetes

### What I learned

This project was an exercise in building a complete, production-grade application with an end-to-end F# stack. Key highlights include:

- **Contract-first API:** Using `Fable.Remoting` with shared F# interfaces (`IProfileApi`) eliminates manual JSON serialization and allows both client and server to pattern-match on shared domain errors (`AppError`).
- **Role-based Authorization:** Implemented admin vs. standard user flows. The backend securely checks cookie claims, allowing admins to modify other users' profiles, while Elmish routes guard unauthorized access client-side.
- **Azure Blob Storage:** Migrated profile image storage from inline base64 strings to Azure Blob Storage using a dedicated multipart endpoint, with a seamless fallback to a local Azurite emulator during development.
- **CI-Driven Builds:** Orchestrated parallel frontend (Bun) and backend (.NET) compilation using `Fun.Build` (F# Make), keeping the final Docker container ultra-lean by injecting pre-built artifacts onto an Alpine runner.

### Continued development

- Add comprehensive test coverage (backend integration tests with Testcontainers, Elmish update unit tests, Playwright E2E tests)
- Local Kubernetes development with Tilt for a production-like inner loop

## Getting started

### Prerequisites

1. [Nix](https://nixos.org/download.html) with [Direnv](https://direnv.net/docs/hook.html) for reproducible tooling
2. [Docker & Docker Compose](https://www.docker.com/products/docker-desktop/) for the database and local blob storage (Azurite)
3. [.NET SDK 10.0](https://dotnet.microsoft.com/download)
4. [Bun](https://bun.sh/) as the javascript runtime/package manager

### Running locally

```bash
git clone https://github.com/wedgehov/link-sharing-app.git
cd link-sharing-app
direnv allow
```

Start the infrastructure (Postgres & Azurite):

```bash
docker compose up -d
```

Restore tools and run the development pipeline (starts both backend API and Vite frontend):

```bash
dotnet tool restore
dotnet fsi build.fsx -p Watch
```

- Frontend: http://localhost:5173
- Backend: http://localhost:5200

A development seed user is created automatically in `Development` mode: `test@example.com` / `secret123` (Admin role).

### Build for Production

To build the project locally (compiles the app to the `/dist` folder):
```bash
dotnet fsi build.fsx -p Bundle
```

## Database migrations

Migrations are generated via the EF Core CLI and never edited manually:

```bash
dotnet ef migrations add YourMigrationName \
  --project Backend/Entity/Entity.csproj \
  --startup-project Backend/Server/backend.fsproj \
  --output-dir Migrations
```

The backend applies pending migrations automatically on startup with retry logic for Docker race conditions.

## Deployment

This repository contains application source code only. Deployment configuration (Helm charts, Kubernetes manifests) is managed in a separate [GitOps repository](https://github.com/wedgehov/gitops) using a rendered-manifests pattern synced by ArgoCD.

## Author

- Frontend Mentor - [@wedgehov](https://www.frontendmentor.io/profile/wedgehov)
