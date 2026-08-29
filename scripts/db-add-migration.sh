#!/usr/bin/env bash
# Generates a new EF Core migration file from the current model. Does not apply it —
# run db-migrate.sh afterward to update the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef).
# The tooling builds the Api host, so it needs the same environment Program.cs does.
# Export it first: set -a && source .env && set +a
# Needs ASPNETCORE_ENVIRONMENT=Development until auth is ready: ICurrentPlayer is only
# registered there.
#
# Usage: scripts/db-add-migration.sh <MigrationName>
set -euo pipefail
cd "$(dirname "$0")/.."

if [ $# -lt 1 ]; then
  echo "Usage: $0 <MigrationName> [additional dotnet ef args...]" >&2
  exit 1
fi

: "${ConnectionStrings__DefaultConnection:?Environment variable ConnectionStrings__DefaultConnection is not set}"

dotnet ef migrations add "$@" \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Api
