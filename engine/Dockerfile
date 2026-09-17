FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Directory.Build.props .
COPY src/Engine.Api/Engine.Api.csproj src/Engine.Api/
RUN dotnet restore src/Engine.Api/Engine.Api.csproj

COPY src/ src/
# StyleCop's rule overrides live in the repo-root .editorconfig, outside this build
# context, so analyzers here would run with the wrong ruleset. Lint runs locally and in CI.
RUN dotnet publish --no-restore -p:RunAnalyzers=false \
  src/Engine.Api/Engine.Api.csproj -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for compose health check
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

# Ordered least- to most-frequently changed: a changed layer invalidates every
# layer after it, so the build output goes last to keep the assets cache-stable.
COPY katago/squashfs-root/usr/bin/katago katago/squashfs-root/usr/bin/katago
COPY katago/squashfs-root/usr/lib/ katago/squashfs-root/usr/lib/
COPY katago/squashfs-root/AppRun katago/squashfs-root/AppRun
COPY models/*.bin.gz models/
COPY config/*.cfg config/

COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "Engine.Api.dll"]