#!/usr/bin/env bash
# Applies pending EF Core migrations to the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef).
# The target database comes from ConnectionStrings__DefaultConnection.
# Export it first: set -a && source .env && set +a
set -euo pipefail
cd "$(dirname "$0")/.."

: "${ConnectionStrings__DefaultConnection:?Environment variable ConnectionStrings__DefaultConnection is not set}"

dotnet ef database update \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Api \
  "$@"
