# Frontend Mentor - Link-sharing app solution

This is a full-stack solution to the [Link-sharing app challenge on Frontend Mentor](https://www.frontendmentor.io/challenges/linksharing-app-Fbt7yweGsT).

This project implements a complete link-sharing application with a modern, end-to-end F# stack, containerized with Docker, and configured with a reproducible Nix development environment. It aims to follow common best practices for development workflows and testing, and can be a useful starting point.

## Table of contents

- [Overview](#overview)
  - [The challenge](#the-challenge)
  - [Screenshot](#screenshot)
  - [Links](#links)
- [My Process & Built With](#my-process--built-with)
- [Deployment & GitOps](#deployment--gitops)
- [Getting Started: The Development Environment](#getting-started-the-development-environment)
  - [Prerequisites](#prerequisites)
  - [Running Locally](#running-locally)
- [Development Workflow & Architecture](#development-workflow--architecture)
  - [Core Philosophy: Contract-First Development](#core-philosophy-contract-first-development)
  - [Development Scripts](#development-scripts)
  - [Public Profile Slugs](#public-profile-slugs)
  - [The Testing Strategy (TODO)](#the-testing-strategy-todo)
- [Database Migrations](#database-migrations)
- [References](#references)

## Overview

### The challenge

Users should be able to:

- Create, read, update, and delete links.
- See live previews of their links on a mobile mockup.
- Drag and drop links to reorder them.
- Add profile details like a profile picture, name, and email.
- Receive form validations for invalid URLs or missing profile details.
- Preview their devlinks profile and copy the link to their clipboard.
- View the optimal layout for the app depending on their device's screen size.
- See hover and focus states for all interactive elements.
- **Bonus**: Create an account and log in (user authentication).
- **Bonus**: Save all details to a database.

### Screenshot

![](./preview.jpg)

### Links

- **Live Site:** (TODO)
- **Live Site (Main):** (TODO)
- **Live Site (Test):** (TODO)
- **Live Site (Production):** (TODO)

## My Process & Built With

This project is a full-stack application built entirely with F# and modern web technologies, emphasizing functional programming, type safety, and a robust development experience.

**Frontend:**

- **F#** with **Fable** to compile to JavaScript
- **Elmish** for state management (The Elm Architecture)
- **Feliz** for a declarative, type-safe React DSL
- **Vite** for a fast development server and build tool
- **Tailwind CSS** for utility-first styling
- **React** as the underlying UI library

**Backend:**

- **F#** with **ASP.NET Core**
- **Giraffe** as a lightweight, functional web framework
- **Fable.Remoting** for typed RPC between client and server
- **FsToolkit.ErrorHandling** for typed, composable domain workflows
- **Entity Framework Core** for data access (Code-First)
- **PostgreSQL** as the relational database
- **Cookie-based Authentication** for session management
- **BCrypt.Net-Next** for secure password hashing

**DevOps & Tooling:**

- **Docker & Docker Compose** for containerization and local environment consistency.
- **Nix** to create a reproducible development environment.
- **Direnv** to automatically load the Nix shell.
- **NPM** for frontend package management and scripts.
- **GitHub Actions** for Continuous Integration: image build/push to GHCR (test jobs TODO).

## Deployment & GitOps

This repository contains the application source code. All deployment configuration, including Helm charts, environment-specific values, and Kubernetes manifests, is managed in a separate **GitOps repository**.

- **GitOps Repository:** [https://github.com/wedgehov/gitops](https://github.com/wedgehov/gitops)

The deployment process follows a "Rendered Manifests" pattern, where Helm charts are pre-rendered into static YAML files. These files are the source of truth that ArgoCD uses to sync the application state to the Kubernetes cluster. This approach ensures that every change to the deployed application is version-controlled, auditable, and happens through a pull request in the `gitops` repository.

## Getting Started: The Development Environment

This project uses Nix and Docker to provide a fully reproducible development environment.

### Prerequisites

1.  **Nix Package Manager:** Nix ensures that every developer uses the exact same versions of all tools (like the .NET SDK and Node.js). Follow the [official installation guide](https://nixos.org/download.html).
2.  **Direnv:** A shell extension that automatically loads the Nix environment when you enter the project directory. Please see the [official documentation](https://direnv.net/docs/hook.html) for installation instructions.
3.  **Docker & Docker Compose:** Required to run the complete application stack, including the PostgreSQL database. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).

### Running Locally

1.  Clone the repository:
    ```bash
    git clone https://github.com/your-username/link-sharing-app.git
    cd link-sharing-app
    ```

2.  Enable Direnv for the project:
    This command approves the loading of the Nix shell defined in `.envrc`. You only need to do this once.
    ```bash
    direnv allow
    ```
    Your shell will now have the correct versions of `.NET`, `node`, `npm`, etc., available.

3.  Run with Docker Compose:
    ```bash
    docker compose up --build
    ```
    Access:
    -   **Frontend:** http://localhost:5173
    -   **Backend (HTTP):** http://localhost:5200

### Local Kubernetes Development with Tilt (TODO)

For a more advanced development workflow that mirrors a production-like Kubernetes setup, a `Tiltfile` can be added. This enables a hybrid development environment:

*   **Backend:** Runs in a pod on a shared development Kubernetes cluster with hot-reloading for code changes (syncing compiled DLLs without rebuilding images).
*   **Frontend:** Runs as a local process on your machine using the Vite dev server for instant HMR.

This setup provides a high-fidelity development environment that closely matches production while maintaining a fast inner loop.

## Development Workflow & Architecture

### Core Philosophy: Contract-First Development

This project is built using a **Contract-First** approach. The `Shared` project defines the API contract (F# record types and interfaces) that acts as a source of truth for frontend-backend communication. This enables parallel development and reduces integration errors.

The app now uses **Fable.Remoting** end-to-end for API communication, with strongly-typed `AppError` domain errors shared across server and client. This removes manual JSON parsing and keeps transport concerns out of the page-level update logic.

### Development Scripts

The client can be run in the following modes:

| Script (`npm run <script>`) | Use Case |
| :--- | :--- |
| `dev` | Default local frontend development mode. |
| `dev:authed` | Starts frontend with development auth state enabled. |
| `build` | Creates a production frontend build. |
| `preview` | Serves the built frontend for local verification. |

The development auth mode works via the `VITE_START_AUTHENTICATED` environment variable in Vite [1].

### Public Profile Slugs

This app uses human-readable profile slugs for public preview URLs.

- On profile creation (or when missing), the server generates a slug from first/last name, falling back to the email local-part or `user-<id>`.
- Slugs are normalized (lowercase, diacritics removed), non-alphanumeric converted to `-`, and made unique by appending an incrementing suffix (`-2`, `-3`, ...).
- Existing slugs stay stable on profile updates to avoid breaking links. If you need to support slug changes, add a redirect table and return `301` for old slugs (not implemented yet).
- The header Preview link uses the current profile slug when available.

Example URLs: `/#/preview/john-appleseed`, `/#/preview/john-appleseed-2`.

### The Testing Strategy (TODO)

*This section describes the testing plan, which is yet to be implemented.*

The goal is a comprehensive suite covering the entire application stack, from pure business logic to browser-based end-to-end flows.

**Backend Testing:**

-   **Unit Tests:** Will test business logic in `Backend/Server` in isolation using an in-memory database provider.
-   **Integration Tests:** Will verify API endpoints against a real PostgreSQL database spun up using Testcontainers.

**Frontend Testing:**

-   **Unit Tests:** Will test the pure Elmish `update` functions to assert correct state transitions without rendering UI.

**End-to-End (E2E) Testing:**

-   Tests will be written in F# using Playwright to simulate real user flows (login, creating links, updating profile) against a live, deployed `test` environment in the Kubernetes cluster.

**How to Run Tests (TODO):**

When implemented, all tests can be run from the root of the repository with:
```bash
dotnet test
```

## Database Migrations

This project uses EF Core for a **Code-First** approach to database management.

-   **Creating Migrations:** When you change an entity model in `Backend/Entity`, create a new migration by running the following command from the repository root:
    ```bash
    dotnet ef migrations add YourMigrationName --project Backend/Entity/Entity.csproj --startup-project Backend/Server/backend.fsproj --output-dir Migrations
    ```
-   **Applying Migrations:**
    - Local (Docker Compose): The backend applies pending migrations automatically on startup (with retry) when running via `docker compose`.
    - Manual (optional during development):
      ```bash
      dotnet ef database update --project Backend/Entity/Entity.csproj --startup-project Backend/Server/backend.fsproj
      ```

## References

[1] Vite contributors, "Env Variables and Modes," *Vite*, [Online]. Available: https://vitejs.dev/guide/env-and-mode.html. [Accessed: Oct. 9, 2025].
