# Built with the SDK, run on the smaller ASP.NET runtime image as a non-root user.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore from the project files alone, so this layer stays cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/FunBooksAndVideos.Domain/FunBooksAndVideos.Domain.csproj src/FunBooksAndVideos.Domain/
COPY src/FunBooksAndVideos.Api/FunBooksAndVideos.Api.csproj src/FunBooksAndVideos.Api/
RUN dotnet restore src/FunBooksAndVideos.Api

COPY src/ src/
RUN dotnet publish src/FunBooksAndVideos.Api --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FunBooksAndVideos.Api.dll"]
