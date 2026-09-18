FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props .
COPY src/GoSharp/GoSharp.csproj src/GoSharp/
COPY src/GoTrainingPlatform.Domain/GoTrainingPlatform.Domain.csproj src/GoTrainingPlatform.Domain/
COPY src/GoTrainingPlatform.Application/GoTrainingPlatform.Application.csproj src/GoTrainingPlatform.Application/
COPY src/GoTrainingPlatform.Infrastructure/GoTrainingPlatform.Infrastructure.csproj src/GoTrainingPlatform.Infrastructure/
COPY src/GoTrainingPlatform.Api/GoTrainingPlatform.Api.csproj src/GoTrainingPlatform.Api/
RUN dotnet restore src/GoTrainingPlatform.Api/GoTrainingPlatform.Api.csproj

COPY src/ src/
# StyleCop's rule overrides live in the repo-root .editorconfig, outside this build
# context, so analyzers here would run with the wrong ruleset. Lint runs locally and in CI.
RUN dotnet publish --no-restore -p:RunAnalyzers=false \
  src/GoTrainingPlatform.Api/GoTrainingPlatform.Api.csproj -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for compose health check
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "GoTrainingPlatform.Api.dll"]