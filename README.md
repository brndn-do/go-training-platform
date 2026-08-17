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

Engine is functional and containerized (suggestion/hint pipeline, health checks). Backend domain/persistence layer is built and tested, but its `Api` layer (HTTP endpoints) doesn't exist yet. Frontend is an empty scaffold. Not yet a playable game end-to-end.

## Setup

**Prerequisites:** .NET 10 SDK, Node.js, Docker (with Compose).

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
Change `KataGoProcess__ExecutablePath`/`ModelPath`/`HumanModelPath`/`ConfigPath` to match where you put the files above (`ExecutablePath` → the `AppRun` from step 1), plus Postgres credentials and a `Jwt__Secret`.

Then, for anything you run **locally** (not via `docker compose`) — including some integration tests — export these into your shell first:
```bash
set -a && source .env && set +a
```
Note: must re-source every time a new shell is opened for this project. We may wish to change this later (direnv or DotNetEnv in code)

### 3. Bring up infra

```bash
scripts/dev-up.sh   # as of now, just postgres + engine, via Docker
```

### 4. Run things

```bash
# Backend
cd backend && dotnet run --project src/GoTrainingPlatform.Api
dotnet test   # Domain.Tests, Application.Tests, Infrastructure.Tests (spins up its own Postgres via Testcontainers — just needs Docker running, not `dev-up.sh`)

# Engine
cd engine && dotnet test   # includes real katago integration tests — needs env vars sourced (step 2), and is slow

# Frontend
cd frontend && npm install && npm run dev
```

`scripts/db-migrate.sh` applies EF Core migrations. It runs against the connection string hardcoded in `GoTrainingPlatformDbContextFactory` (the design-time factory `dotnet ef` uses), not the one in your `.env` — so keep the two matching, or edit the factory. `scripts/db-reset.sh` wipes local Postgres data and re-migrates.
