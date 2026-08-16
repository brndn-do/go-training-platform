FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/Engine.Api/Engine.Api.csproj src/Engine.Api/
RUN dotnet restore src/Engine.Api/Engine.Api.csproj

COPY src/ src/
RUN dotnet publish --no-restore src/Engine.Api/Engine.Api.csproj -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for compose health check
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

COPY katago/squashfs-root/usr/bin/katago katago/squashfs-root/usr/bin/katago
COPY katago/squashfs-root/usr/lib/ katago/squashfs-root/usr/lib/
COPY katago/squashfs-root/AppRun katago/squashfs-root/AppRun
COPY config/*.cfg config/
COPY models/*.bin.gz models/

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "Engine.Api.dll"]