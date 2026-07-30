#!/usr/bin/env bash
# Tears down the whole compose stack (removing volumes), brings postgres back up,
# and re-applies migrations. WARNING: destroys all local data in the postgres volume.
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose down --volumes
docker compose up -d postgres

echo "Waiting for postgres to become healthy..."
until [ "$(docker compose ps -q postgres | xargs docker inspect -f '{{.State.Health.Status}}')" = "healthy" ]; do
  sleep 1
done

"$(dirname "$0")/db-migrate.sh"
