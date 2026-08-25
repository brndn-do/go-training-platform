# Shared argument handling for scripts/test-backend.sh and scripts/test-engine.sh.
# Source this, then call: run_dotnet_test <default-solution> "$@"
#
# Modes (default: everything):
#   --unit         only tests with no external dependency (Category!=Integration)
#   --integration  only tests that touch Docker, the engine service, or KataGo
#   --all          everything (the default)
#   --coverage     also collect Cobertura coverage via coverlet.msbuild
#
# A path ending in .csproj or .slnx replaces the stack's default solution, so a
# single test project can be run on its own. Every other argument is forwarded to
# `dotnet test` untouched — including a hand-written --filter, which only works
# alongside --all since the mode flags set --filter themselves.

run_dotnet_test() {
  local target="$1"
  shift

  local filter=""
  local coverage=0
  local forwarded=()

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --unit) filter="Category!=Integration" ;;
      --integration) filter="Category=Integration" ;;
      --all) filter="" ;;
      --coverage) coverage=1 ;;
      *.csproj | *.slnx) target="$1" ;;
      *) forwarded+=("$1") ;;
    esac
    shift
  done

  local args=("$target" -tl:off)

  if [[ -n "$filter" ]]; then
    args+=(--filter "$filter")
  fi

  if [[ "$coverage" -eq 1 ]]; then
    args+=(/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura)
  fi

  dotnet test "${args[@]}" ${forwarded[@]+"${forwarded[@]}"}
}
