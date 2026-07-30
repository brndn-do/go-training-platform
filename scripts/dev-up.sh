#!/usr/bin/env bash
# Brings up infra only (postgres + engine); run backend/frontend locally for fast iteration.
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose up -d postgres engine
