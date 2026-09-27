#!/usr/bin/env bash
# Brings up every compose service (postgres, engine, backend); run the frontend locally.
# Rebuilds the images: compose reuses an existing one however stale, and an engine
# serving an old contract fails quietly — a renamed field just deserializes to a default.
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose up -d --build
