#!/usr/bin/env bash
# Runs backend tests with code coverage collection and prints a summary table
# (uses coverlet.msbuild, already referenced in both test projects). Coverage is
# written in Cobertura format so it can be fed into ReportGenerator or read
# directly by editor tooling like VS Code's Coverage Gutters extension.
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet test backend/GoTrainingPlatform.slnx /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura -tl:off "$@"
