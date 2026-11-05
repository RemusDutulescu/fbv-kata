# One build, two images: --target api (the default) or --target worker. Both run as a non-root user.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore from the project files alone, so this layer stays cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/FunBooksAndVideos.Domain/FunBooksAndVideos.Domain.csproj src/FunBooksAndVideos.Domain/
COPY src/FunBooksAndVideos.Infrastructure/FunBooksAndVideos.Infrastructure.csproj src/FunBooksAndVideos.Infrastructure/
COPY src/FunBooksAndVideos.Application/FunBooksAndVideos.Application.csproj src/FunBooksAndVideos.Application/
COPY src/FunBooksAndVideos.Api/FunBooksAndVideos.Api.csproj src/FunBooksAndVideos.Api/
COPY src/FunBooksAndVideos.Worker/FunBooksAndVideos.Worker.csproj src/FunBooksAndVideos.Worker/
RUN dotnet restore src/FunBooksAndVideos.Api && dotnet restore src/FunBooksAndVideos.Worker

COPY src/ src/
RUN dotnet publish src/FunBooksAndVideos.Api --configuration Release --no-restore --output /app/api \
 && dotnet publish src/FunBooksAndVideos.Worker --configuration Release --no-restore --output /app/worker

# The worker serves no HTTP, so it runs on the plain .NET runtime.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
WORKDIR /app
COPY --from=build /app/worker .
USER $APP_UID
ENTRYPOINT ["dotnet", "FunBooksAndVideos.Worker.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/api .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FunBooksAndVideos.Api.dll"]
