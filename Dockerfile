# Stage 1: Build the frontend assets
FROM node:25-slim AS frontend-build

# Install .NET SDK 9, required by vite-plugin-fable.
RUN apt-get update && apt-get install -y curl libicu-dev && rm -rf /var/lib/apt/lists/*
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir /usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}"

WORKDIR /app/Client

# Install client dependencies first to maximize Docker layer cache reuse.
COPY Client/package.json Client/package-lock.json* ./
RUN npm install && npm rebuild

# Copy the client source and shared project used by Fable.
COPY Client .
COPY Shared /app/Shared

# Build frontend assets.
RUN npm run build

# Stage 2: Build backend and migrations
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

# Copy solution and project files first for better restore caching.
COPY LinkSharingApp.sln .
COPY Backend/Server/backend.fsproj Backend/Server/
COPY Backend/Entity/Entity.csproj Backend/Entity/
COPY Shared/Shared.fsproj Shared/
COPY Client/src/src.fsproj Client/src/

# Restore dependencies.
RUN dotnet restore

# Copy the rest of the source code.
COPY . .

# Publish backend.
RUN dotnet publish Backend/Server/backend.fsproj -c Release -o /app/publish

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published backend.
COPY --from=backend-build /app/publish .

# Copy built frontend to wwwroot for static hosting by backend.
COPY --from=frontend-build /app/Client/dist ./wwwroot

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "backend.dll"]
