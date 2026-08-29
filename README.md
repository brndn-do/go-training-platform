# Go Training Platform

Play Go (Baduk/Weiqi) against bots of varying strength, with optional live hints and undo, powered by KataGo.

## Stack
- Frontend: React (Vite)
- Backend: ASP.NET Core (Clean Architecture, 4 layers)
- ORM: Entity Framework Core
- Database: PostgreSQL (Docker)
- Engine: KataGo, wrapped by a separate .NET microservice (its own process/IO layer, not layered like the backend)

## Architecture

Three independently-versioned stacks: `backend/`, `engine/`, `frontend/`. The backend talks to the engine over HTTP; the engine wraps the `katago` binary as a child process. See `docs/architecture/decisions/` (ADRs) for why things are structured this way.

## Status

Playable end to end. The backend's four layers and its seven HTTP endpoints are built and tested, the engine's suggestion/hint and health-check pipeline is functional and containerized, and a full game has been played over HTTP against real Postgres and a real engine. The frontend is still an empty scaffold.

The backend only starts in the Development environment. There is no authentication yet, so the stand-in `ICurrentPlayer` is registered only under `IsDevelopment()` and startup fails anywhere else. That is deliberate, and it blocks deployment until auth ships.

## Setup

**Prerequisites:** .NET 10 SDK, EF Core tools, Node.js, Docker (with Compose).

### 1. Get KataGo

The `katago` binary and its neural net model files aren't in git (`engine/katago/`, `engine/models/` are gitignored — large, platform-specific). Download them yourself from [KataGo's releases](https://github.com/lightvector/KataGo/releases):

- Binary → `engine/katago/`. If it's an AppImage (the common case on Linux), extract it instead of running it directly — containers (and this repo's own tooling) can't rely on the AppImage's FUSE self-mount:
  ```bash
  cd engine/katago && chmod +x katago && ./katago --appimage-extract
  ```
  This produces `engine/katago/squashfs-root/`; `AppRun` inside it is what you point config at, not the raw binary.
- Models → `engine/models/`. This project uses `kata1-zhizi-b40c768nbt-s11272M-d5935M.bin.gz` (`ModelPath`) and `b18c384nbt-humanv0.bin.gz` (`HumanModelPath`, a human-SL model — required specifically, not just any KataGo network, since ranked bot strengths below Superhuman rely on that model line's `humanSLProfile` support).

### 2. Configure environment

```bash
cp .env.example .env
```
Change `KataGoProcess__ExecutablePath`/`ModelPath`/`HumanModelPath`/`ConfigPath` to match where you put the files above (`ExecutablePath` → the `AppRun` from step 1), plus Postgres credentials, a `CurrentPlayer__Id` (any GUID — the backend refuses to start without one), and a `Jwt__Secret`.

The test suites read `.env` themselves, so they need no shell setup. Anything else you run **locally** rather than through `docker compose` — `dotnet run`, `scripts/db-add-migration.sh`, `scripts/db-migrate.sh` — still needs it exported first, per shell:
```bash
set -a && source .env && set +a
```
A variable already set in your shell beats the file, so exporting one for a single command overrides it.

### 3. Bring up infra

```bash
scripts/dev-up.sh   # as of now, just postgres + engine, via Docker
```

### 4. Run things

```bash
# Backend
cd backend && dotnet run --project src/GoTrainingPlatform.Api

# Frontend
cd frontend && npm install && npm run dev

# Tests — from the repo root, no shell setup needed
scripts/test-backend.sh --unit          # ~1s
scripts/test-backend.sh --integration   # needs Docker, and the engine up for four of them
scripts/test-engine.sh --unit           # ~0.4s
scripts/test-engine.sh --integration    # needs katago from step 1
```

Each script takes `--unit`, `--integration`, `--all` (the default) and `--coverage`, forwards anything else to `dotnet test`, and accepts a `.csproj` path to run one project alone.

`Infrastructure.Tests` provisions its own Postgres via Testcontainers, so it needs Docker running but not `dev-up.sh`. Its engine integration tests do need a running engine (`dev-up.sh`, or `dotnet run --project src/Engine.Api` from `engine/`) — without one, four tests fail with a message saying whether it is unreachable or merely not ready yet.

`scripts/db-add-migration.sh <Name>` generates a new EF Core migration file from the current model, without applying it. `scripts/db-migrate.sh` applies whatever migrations already exist. Both build the `Api` host to find `ConnectionStrings__DefaultConnection` and the rest of its configuration, so both need step 2's environment exported first, plus `ASPNETCORE_ENVIRONMENT=Development` until auth ships. `scripts/db-reset.sh` wipes local Postgres data and re-migrates.
