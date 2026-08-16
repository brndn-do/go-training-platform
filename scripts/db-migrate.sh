#!/usr/bin/env bash
# Applies pending EF Core migrations to the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef).
# The target database comes from GoTrainingPlatformDbContextFactory's hardcoded
# design-time connection string, not from .env — EF tooling prefers that factory
# over building the Api host.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet ef database update \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Api \
  "$@"
