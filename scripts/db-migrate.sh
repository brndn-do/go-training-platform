#!/usr/bin/env bash
# Applies pending EF Core migrations to the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef) and EF Core
# wiring in GoTrainingPlatform.Infrastructure (not added yet — this is a scaffold).
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet ef database update \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Api \
  "$@"
