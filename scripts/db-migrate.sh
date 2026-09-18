#!/usr/bin/env bash
# Applies pending EF Core migrations to the database.
# Requires `dotnet-ef` (dotnet tool install --global dotnet-ef).
# The target database comes from ConnectionStrings__DefaultConnection.
# Export it first: set -a && source .env && set +a
#
# Pass --prompt-connection to be prompted for the connection string, overriding
# whatever the environment holds for this run only. Use it for databases whose
# credentials you do not want written to .env, such as a remote host. The rest of
# the environment still has to be exported -- the tooling boots the Api host,
# which validates its own configuration before any migration runs.
set -euo pipefail
cd "$(dirname "$0")/.."

prompt_connection=false
args=()
for arg in "$@"; do
  if [ "$arg" = "--prompt-connection" ]; then
    prompt_connection=true
  else
    args+=("$arg")
  fi
done

if [ "$prompt_connection" = true ]; then
  read -rsp "Connection string: " connection
  echo
  [ -n "$connection" ] || { echo "No connection string entered." >&2; exit 1; }

  # A pasted string often carries the quotes it was wrapped in. Npgsql reads a leading
  # quote as the start of a keyword and then fails at the far end of the string.
  connection=${connection#[\"\']}
  connection=${connection%[\"\']}

  export ConnectionStrings__DefaultConnection="$connection"
fi

: "${ConnectionStrings__DefaultConnection:?Environment variable ConnectionStrings__DefaultConnection is not set}"

dotnet ef database update \
  --project backend/src/GoTrainingPlatform.Infrastructure \
  --startup-project backend/src/GoTrainingPlatform.Api \
  ${args[@]+"${args[@]}"}
