FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

# Install krb5-libs to fix "Cannot load library libgssapi_krb5.so.2" from Npgsql
RUN apk add --no-cache krb5-libs

COPY dist/ /app
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

CMD ["dotnet", "backend.dll"]
