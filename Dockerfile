FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

COPY dist/ /app
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

CMD dotnet backend.dll
