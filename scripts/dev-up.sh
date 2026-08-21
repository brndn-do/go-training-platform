#!/usr/bin/env bash
# Brings up infra only (postgres + engine); run backend/frontend locally for fast iteration.
# Rebuilds the engine image: compose reuses an existing one however stale, and an engine
# serving an old contract fails quietly — a renamed field just deserializes to a default.
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose up -d --build postgres engine
