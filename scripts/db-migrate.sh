#!/usr/bin/env bash
# Applies pending EF Core migrations to the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef).
# The target database comes from ConnectionStrings__DefaultConnection, read by
# GoTrainingPlatformDbContextFactory — EF tooling prefers that factory over
# building the Api host. Export it first: set -a && source .env && set +a
# Infrastructure is its own startup project: Api doesn't reference
# Microsoft.EntityFrameworkCore.Design, which the tooling requires there.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet ef database update \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Infrastructure \
  "$@"
