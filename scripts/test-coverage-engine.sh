#!/usr/bin/env bash
# Runs engine tests with code coverage collection (uses coverlet.msbuild,
# already referenced in Engine.Api.Tests). Coverage is written in Cobertura
# format so it can be fed into ReportGenerator or read directly by editor
# tooling like VS Code's Coverage Gutters extension.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet test engine/Engine.slnx /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura -tl:off "$@"
