# Stage 1: Build the frontend assets
FROM node:22-slim AS build

# Install .NET SDK 9 using the official install script, required by vite-plugin-fable.
# We switch to a Debian-based image (slim) to avoid esbuild native binary issues on Alpine/musl.
RUN apt-get update && apt-get install -y curl libicu-dev && rm -rf /var/lib/apt/lists/*
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir /usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}"

WORKDIR /app/src/Client

# Copy package files and install dependencies (works with or without lockfile)
COPY src/Client/package*.json ./
RUN npm install && npm rebuild

# Copy the rest of the application source code AFTER installing dependencies
COPY src/Client .
# Also copy Shared project so vite-plugin-fable can resolve ../../Shared/Shared.fsproj
COPY src/Shared /app/src/Shared

# Build the application using Vite
RUN npm run build

# Stage 2: Serve the static files with Nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Install 'gettext' which provides the 'envsubst' utility for variable substitution
RUN apk add --no-cache gettext

# Remove default Nginx content
RUN rm -rf ./*

# Copy built assets from the build stage
COPY --from=build /app/src/Client/dist .

# Copy our custom Nginx configuration template and the entrypoint script
COPY src/Client/nginx.conf /etc/nginx/conf.d/default.conf.template
COPY src/Client/custom-entrypoint.sh /custom-entrypoint.sh
RUN chmod +x /custom-entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/custom-entrypoint.sh"]
