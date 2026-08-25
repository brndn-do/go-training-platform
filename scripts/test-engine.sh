#!/usr/bin/env bash
# Runs the engine test suite. See scripts/lib/dotnet-test.sh for the flags.
#   --unit         seconds, no KataGo needed
#   --integration  needs the gitignored katago binary and models, and the paths in
#                  .env. Slow: every test starts a real KataGo that loads ~919MB of
#                  models.
#
# Don't run this at the same time as scripts/test-backend.sh --integration, and stop
# the engine container first (docker compose stop engine) — xUnit only serialises
# within one assembly, so a second KataGo elsewhere on the machine competes for the
# memory and CPU these tests need, which is what makes their timeouts flaky.
set -euo pipefail
cd "$(dirname "$0")/.."

source scripts/lib/dotnet-test.sh

run_dotnet_test engine/Engine.slnx "$@"
