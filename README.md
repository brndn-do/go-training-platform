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

Register, login, logout, and a `me` session check are built, and the game endpoints require a signed-in user.

## Setup

**Prerequisites:** .NET 10 SDK, EF Core tools (`dotnet tool install --global dotnet-ef`), Node.js, Docker (with Compose), plus `unzip` for step 2.

### 1. Configure environment

```bash
cp .env.example .env
```

Set `REPO_ROOT` to the absolute path of your clone — every on-disk path in the file derives from it, so nothing else needs editing unless you change Postgres credentials, ports, or the KataGo model files from step 2.

The test suites read `.env` themselves, so they need no shell setup. Anything else you run **locally** rather than through `docker compose` — `dotnet run`, `scripts/db-add-migration.sh`, `scripts/db-migrate.sh` — still needs it exported first, per shell:
```bash
set -a && source .env && set +a
```
A variable already set in your shell beats the file, so exporting one for a single command overrides it.

**Recommended: [direnv](https://direnv.net/).** It loads and unloads `.env` automatically as you enter and leave the directory, so you never hit a confusing failure from a shell you forgot to set up. The repo ships a one-line `.envrc`; install direnv, hook it into your shell, then approve the file once:
```bash
direnv allow .
```
direnv expands `${...}` the same way Bash, Docker Compose and the test assemblies do, so it sees identical values to every other consumer of the file.

### 2. Get KataGo

The `katago` binary and its neural net model files aren't in git (`engine/katago/`, `engine/models/` are gitignored — large, platform-specific). Download them yourself.

#### Binary → `engine/katago/`

From [KataGo's releases](https://github.com/lightvector/KataGo/releases), download a Linux Eigen (pure-CPU) build. This project uses `v1.18.1`: [`katago-v1.18.1-eigenavx2-linux-x64.zip`](https://github.com/lightvector/KataGo/releases/download/v1.18.1/katago-v1.18.1-eigenavx2-linux-x64.zip). `eigenavx2` needs a CPU with AVX2 + FMA — if `grep -o avx2 /proc/cpuinfo` comes up empty, take the `eigen` build instead. Not every release publishes Eigen builds (some ship CUDA/TensorRT only), so check a release's Assets before switching versions.

The binary is an AppImage, which self-mounts via FUSE at startup — that fails in a container, so extract it once:

```bash
unzip path/to/katago-v1.18.1-eigenavx2-linux-x64.zip -d engine/katago
(cd engine/katago && chmod +x katago && ./katago --appimage-extract)
engine/katago/squashfs-root/AppRun version   # verify
```

`AppRun` inside `squashfs-root/` is what config points at, not the raw binary.

#### Models → `engine/models/`

- **Self-play network** — from [katagotraining.org/networks](https://katagotraining.org/networks/). This project uses [`kata1-zhizi-b40c768nbt-s11472M-d5982M.bin.gz`](https://media.katagotraining.org/uploaded/networks/models/kata1/kata1-zhizi-b40c768nbt-s11472M-d5982M.bin.gz). Prefer the convolutional families (`b40c768nbt`, `b28c512nbt`) over the `tf3-*` transformers: on CPU a transformer takes much longer per position.
- **Human SL network** — from [katagotraining.org/extra_networks](https://katagotraining.org/extra_networks/): [`b18c384nbt-humanv0.bin.gz`](https://media.katagotraining.org/uploaded/networks/models_extra/b18c384nbt-humanv0.bin.gz).

Keep the downloaded filenames. `.env` names each one in `KATAGO_MODEL_FILE`/`KATAGO_HUMAN_MODEL_FILE`, so using a different network means changing that variable. When you swap one, delete the old file — the engine image copies every `.bin.gz` in `engine/models/`.

### 3. Bring up infra

```bash
scripts/dev-up.sh                        # as of now, just postgres + engine, via Docker
set -a && source .env && set +a          # the migration tooling runs locally, so it needs this
scripts/db-migrate.sh                    # create the schema in the fresh postgres volume
```

The engine container answers its health check only once KataGo has loaded both models. `docker compose ps` reports `health: starting` until then.

### 4. Run things

```bash
# Backend
cd backend && dotnet run --project src/GoTrainingPlatform.Api

# Frontend
cd frontend && npm install && npm run dev

# Tests — from the repo root, no shell setup needed
scripts/test-backend.sh --unit          # Fast
scripts/test-backend.sh --integration   # Slow; needs Docker and the engine up for four of them
scripts/test-engine.sh --unit           # Fast
scripts/test-engine.sh --integration    # Very slow; needs katago from step 2, and the engine container stopped
```

Each script takes `--unit`, `--integration`, `--all` (the default) and `--coverage`, forwards anything else to `dotnet test`, and accepts a `.csproj` path to run one project alone.

`Infrastructure.Tests` provisions its own Postgres via Testcontainers, so it needs Docker running but not `dev-up.sh`. Its engine integration tests need a running engine (`dev-up.sh`).

`scripts/db-add-migration.sh <Name>` generates a new EF Core migration file from the current model, without applying it. `scripts/db-migrate.sh` applies whatever migrations already exist. `scripts/db-reset.sh` wipes local Postgres data and re-migrates.

`scripts/db-migrate.sh --prompt-connection` prompts for a connection string instead of taking it from the environment, for a database whose credentials shouldn't be written to `.env`. The rest of the environment still has to be exported — the tooling boots the Api host, which validates its own configuration before any migration runs.
