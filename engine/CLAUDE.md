# CLAUDE.md — Engine

A .NET microservice wrapping the KataGo binary as a child process. The backend is its only client; it is not publicly reachable and has no auth of its own.

## Structure

Flat, not layered. One project (`src/Engine.Api/`) with three folders, each carrying its own file:

| File                                                                     | Covers                                                      |
| ------------------------------------------------------------------------ | ----------------------------------------------------------- |
| [src/Engine.Api/Analysis/CLAUDE.md](src/Engine.Api/Analysis/CLAUDE.md)   | Query construction, response interpretation, strength tiers |
| [src/Engine.Api/Processes/CLAUDE.md](src/Engine.Api/Processes/CLAUDE.md) | Owning and talking to the child process                     |
| [src/Engine.Api/Endpoints/CLAUDE.md](src/Engine.Api/Endpoints/CLAUDE.md) | The minimal-API surface and the health checks               |

`katago/` and `models/` are gitignored — dropped in locally per the root `README.md`. `config/` is tracked, holding the project-authored analysis-engine config.

## Commands

```
dotnet build --no-incremental
dotnet test                          # everything, including slow KataGo tests
dotnet run --project src/Engine.Api
```

From the repo root, `scripts/test-engine.sh --unit` skips the katago tests; `--integration` runs only those.

The test assembly loads the repo-root `.env` itself via a module initializer, so `KataGoProcess__ExecutablePath`/`ConfigPath`/`ModelPath`/`HumanModelPath` need no exporting — but the katago binary and models those paths point at are still machine-local, so the integration tests fail without them. An already-set variable wins over `.env`.

The two classes that start katago share `[Collection("KataGo")]` so they never run at once: each process is memory-hungry, and running two at once can OOM-kill them and cause flaky timeouts. For the same reason, stop the engine container before running them.

## KataGo configuration in force

Set in `config/go_training_platform_config.cfg` and assumed by the code. [README.md](README.md) covers the research behind these values and what is still unverified; the `.cfg`'s inline comments cover each setting.

- `maxVisits = 1` for every strength, including the top tier — no search anywhere.
- Two models loaded at once: a strong self-play network and a human-SL network.
- One query at a time per instance: `numAnalysisThreads = 1`, `numSearchThreadsPerAnalysisThread = 1`, `nnMaxBatchSize = 1`.
- Win rates are always Black's (`reportAnalysisWinratesAs = BLACK`). Rules are always `chinese`.

## Containerizing

- The downloaded `katago` binary is an AppImage. It self-mounts via FUSE at startup, which works locally (with FUSE) but fails in a container. Fix: `./katago --appimage-extract` once, then `COPY` the extracted `squashfs-root/`. `AppRun` resolves its own location via `readlink -f`, so no code change is needed.
- Scope `.dockerignore` build-output patterns to `src/**/bin/`, not `**/bin/` (so `katago/squashfs-root/usr/bin/ isn't ignored)
