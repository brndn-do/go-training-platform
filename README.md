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
Change `KataGoProcess__ExecutablePath`/`ModelPath`/`HumanModelPath`/`ConfigPath` to match where you put the files above (`ExecutablePath` → the `AppRun` from step 1), plus Postgres credentials, a `CurrentPlayer__Id` (any GUID — the backend refuses to start without one), and a `Jwt__Secret`.

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
dotnet test   # Domain, Application, Infrastructure, and Api tests

# Engine
cd engine && dotnet test   # includes real katago integration tests — needs env vars sourced (step 2), and is slow

# Frontend
cd frontend && npm install && npm run dev
```

`Infrastructure.Tests` provisions its own Postgres via Testcontainers, so it needs Docker running but not `dev-up.sh`. Its engine integration tests do need a running engine (`dev-up.sh`, or `dotnet run --project src/Engine.Api` from `engine/`) plus the env vars from step 2 — without both, four tests fail.

`scripts/db-migrate.sh` applies EF Core migrations, against `ConnectionStrings__DefaultConnection` from your environment (step 2) — the design-time factory `dotnet ef` uses reads it from there. `scripts/db-reset.sh` wipes local Postgres data and re-migrates.
