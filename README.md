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

Playable end to end. The backend's four layers and its HTTP endpoints are built and tested, the engine's suggestion/hint and health-check pipeline is functional and containerized, and a full game has been played over HTTP against real Postgres and a real engine. The frontend is still an empty scaffold.

Register, login, and logout are built, and the game endpoints require a signed-in user.

## Setup

**Prerequisites:** .NET 10 SDK, Node.js, Docker (with Compose). `scripts/db-migrate.sh` and
`scripts/db-add-migration.sh`/`scripts/db-reset.sh` also need the EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Get KataGo

The `katago` binary and its neural net model files aren't in git (`engine/katago/`, `engine/models/` are gitignored — large, platform-specific). You need three files, and they do **not** all come from the same place:

- **Binary** → `engine/katago/`, from [KataGo's releases](https://github.com/lightvector/KataGo/releases). The Linux assets are `.zip` archives named per compute backend — pick one your hardware can actually run: `eigen` (plain CPU) or `eigenavx2` (CPU with AVX2) if you have no usable GPU, `opencl` or a `cuda`/`trt` build if you do. Unzip it; the executable inside is named `katago`, with no file extension.

  That executable is an AppImage. Extract it instead of running it directly — containers (and this repo's own tooling) can't rely on the AppImage's FUSE self-mount:
  ```bash
  cd engine/katago && chmod +x katago && ./katago --appimage-extract
  ```
  This produces `engine/katago/squashfs-root/`; `AppRun` inside it is what you point config at, not the raw binary.
- **Models** → `engine/models/`. They come from two different places, neither of them the release you just downloaded the binary from:
  - `kata1-zhizi-b40c768nbt-s11272M-d5935M.bin.gz` (`ModelPath`) — a kata1 training net, published at [katagotraining.org](https://katagotraining.org/networks/) (~820 MB).
  - `b18c384nbt-humanv0.bin.gz` (`HumanModelPath`, ~95 MB) — attached to the [v1.15.0 release](https://github.com/lightvector/KataGo/releases/tag/v1.15.0). A human-SL model, required specifically, not just any KataGo network, since ranked bot strengths below Superhuman rely on that model line's `humanSLProfile` support.

### 2. Configure environment

```bash
cp .env.example .env
```
Change `KataGoProcess__ExecutablePath`/`ModelPath`/`HumanModelPath`/`ConfigPath` to match where you put the files above (`ExecutablePath` → the `AppRun` from step 1), plus Postgres credentials.

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
# Backend — listens on http://localhost:5270 (see the port note below)
cd backend && dotnet run --project src/GoTrainingPlatform.Api

# Frontend
cd frontend && npm install && npm run dev

# Tests — from the repo root, no shell setup needed
scripts/test-backend.sh --unit          # ~1s
scripts/test-backend.sh --integration   # needs Docker, and the engine up for four of them
scripts/test-engine.sh --unit           # ~0.4s
scripts/test-engine.sh --integration    # needs katago from step 1; several minutes
```

Each script takes `--unit`, `--integration`, `--all` (the default) and `--coverage`, forwards anything else to `dotnet test`, and accepts a `.csproj` path to run one project alone.

**Ports differ between running locally and running under compose.** Run locally, each service takes its port from its own `Properties/launchSettings.json`, *not* from `.env`: the backend comes up on <http://localhost:5270> and the engine on <http://localhost:5120>. The `.env` values are the compose ports — `BACKEND_PORT` (5000) maps the backend container, and `Engine__BaseUrl` (5100) is where the backend and its tests look for the engine. Override the local port with `--urls` when something needs to reach a service where `.env` says it is:

```bash
dotnet run --project src/Engine.Api --urls http://localhost:5100
```

Setting `ASPNETCORE_URLS` in the environment will **not** do it — the launch profile's `applicationUrl` is applied over it, and the service still binds 5120. `--urls` wins because command-line arguments outrank both.

The engine reports unhealthy on `/health/ready` until KataGo has finished loading the model — around a minute, and longer on a CPU-only build. It gets there on its own; `POST /warmup` only front-loads that work (ADR 17), it is not required for the service to become healthy.

`Infrastructure.Tests` provisions its own Postgres via Testcontainers, so it needs Docker running but not `dev-up.sh`. Its engine integration tests do need a running engine, reachable at `Engine__BaseUrl` — either `dev-up.sh`, or from `engine/`, `dotnet run --project src/Engine.Api --urls http://localhost:5100` (a bare `dotnet run` binds 5120 and the tests will not find it). Without one, four tests fail with a message saying whether it is unreachable or merely not ready yet.

`scripts/db-add-migration.sh <Name>` generates a new EF Core migration file from the current model, without applying it. `scripts/db-migrate.sh` applies whatever migrations already exist. There is no design-time `DbContext` factory; both scripts build the `Api` host itself to resolve the `DbContext` and find `ConnectionStrings__DefaultConnection` and the rest of its configuration, which is why they pass `--startup-project` and why both need step 2's environment exported first. `scripts/db-reset.sh` wipes local Postgres data and re-migrates.
