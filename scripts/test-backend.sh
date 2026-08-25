#!/usr/bin/env bash
# Runs the backend test suite. See scripts/lib/dotnet-test.sh for the flags.
#   --unit         seconds, no Docker needed
#   --integration  needs a Docker daemon (Testcontainers starts its own Postgres)
#                  and, for the EngineClient tests, the engine container already up
#                  (scripts/dev-up.sh).
set -euo pipefail
cd "$(dirname "$0")/.."

source scripts/lib/dotnet-test.sh

run_dotnet_test backend/GoTrainingPlatform.slnx "$@"
