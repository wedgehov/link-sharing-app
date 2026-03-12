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
- [Vite 7](https://vitejs.dev/) with [vite-plugin-fable](https://github.com/nicoschi/vite-plugin-fable)
- [Tailwind CSS 4](https://tailwindcss.com/) with custom design tokens

**Backend:**

- [F#](https://fsharp.org/) with [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) (.NET 9)
- [Giraffe](https://github.com/giraffe-fsharp/Giraffe) as a functional web framework
- [Fable.Remoting](https://zaid-ajaj.github.io/Fable.Remoting/) for typed RPC between client and server
- [FsToolkit.ErrorHandling](https://github.com/demystifyfp/FsToolkit.ErrorHandling) for composable error workflows
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) with PostgreSQL (Code-First)
- Cookie-based authentication with role claims and [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) for password hashing

**DevOps:**

- Docker & Docker Compose
- Nix for reproducible development environments
- GitHub Actions for CI (image build/push to GHCR)
- ArgoCD + Helm for GitOps deployment to Kubernetes

### What I learned

This project was an exercise in building a complete, production-grade application with an end-to-end F# stack. A few highlights:

**Contract-first development with Fable.Remoting.** The `Shared` project defines F# interfaces like `IProfileApi` and `ILinkApi` that serve as the single source of truth for both client and server. Fable.Remoting generates typed proxies from these, so there is no manual JSON serialization and domain errors (`AppError`) are shared discriminated unions that the client can pattern-match on directly.

**Elmish architecture at scale.** Each page is an isolated `Model`/`Msg`/`update`/`view` module, composed through a root `Program.fs` that handles routing, auth state, and inter-page coordination. Side effects like toast auto-dismiss timers and clipboard writes are modeled as Elmish commands, keeping the update functions pure.

**Role-based authorization.** The app supports `Standard` and `Admin` roles. The server-side `requireAuthorization` helper checks cookie claims and allows admins to operate on any user's resources. A key detail: when an admin edits another user's profile, operations (like email uniqueness checks) execute against the target user's id, not the admin's own id.

**Client-side route guards.** Rather than letting unauthorized API calls fail with 401s, the Elmish root intercepts private route transitions before any page loads. Unauthenticated users are redirected to login with a toast; authenticated users trying to access another user's page see an error toast and stay on their current page. Admins bypass this check entirely.

### Continued development

- Migrate profile image storage from inline base64/data URLs to Azure Blob Storage with server-mediated uploads
- Add comprehensive test coverage (backend integration tests with Testcontainers, Elmish update unit tests, Playwright E2E tests)
- Local Kubernetes development with Tilt for a production-like inner loop

## Getting started

### Prerequisites

1. [Nix](https://nixos.org/download.html) with [Direnv](https://direnv.net/docs/hook.html) for reproducible tooling
2. [Docker & Docker Compose](https://www.docker.com/products/docker-desktop/) for the database

### Running locally

```bash
git clone https://github.com/wedgehov/link-sharing-app.git
cd link-sharing-app
direnv allow
```

Start the database, backend, and frontend:

```bash
docker compose up -d db
dotnet run --project Backend/Server/backend.fsproj
cd Client && npm install && npm run dev
```

- Frontend: http://localhost:5173
- Backend: http://localhost:5200

Or run everything via Docker Compose:

```bash
docker compose up --build
```

- Unified app: http://localhost:5200

A development seed user is created automatically in `Development` mode: `test@example.com` / `secret123` (Admin role).

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
