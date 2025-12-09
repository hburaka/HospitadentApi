# .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY HospitadentApi.WebService/*.csproj ./HospitadentApi.WebService/
COPY HospitadentApi.Entity/*.csproj ./HospitadentApi.Entity/
COPY HospitadentApi.Repository/*.csproj ./HospitadentApi.Repository/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish
WORKDIR /src/HospitadentApi.WebService
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port (Dokploy will override this if needed)
EXPOSE 8080

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
# Disable file watching in Docker container (prevents inotify limit errors)
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_DETAILEDERRORS=false

# Entry point
ENTRYPOINT ["dotnet", "HospitadentApi.WebService.dll"]

