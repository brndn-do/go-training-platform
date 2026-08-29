# CLAUDE.md — Go Training Platform

Repo-wide guidance. For more detailed guidance, read:

| File                                     | Covers                                                     |
| ---------------------------------------- | ---------------------------------------------------------- |
| [backend/CLAUDE.md](backend/CLAUDE.md)   | ASP.NET Core Web API, Clean Architecture, EF Core/Postgres |
| [engine/CLAUDE.md](engine/CLAUDE.md)     | The KataGo microservice and its child-process plumbing     |
| [frontend/CLAUDE.md](frontend/CLAUDE.md) | React + Vite SPA                                           |
| [docs/CLAUDE.md](docs/CLAUDE.md)         | ADRs, scope, and what's decided vs. still open             |

## What this is

Play Go (Baduk/Weiqi) against bots of varying strength, with optional live hints and undo, powered by KataGo.

`docs/GOALS.md` is the source of truth for v1 scope and non-goals — not this file, and not `README.md`.

Three independently-versioned stacks in one repository, each with its own build and test tooling. Only shared config lives at the root.

## Project structure

```
backend/    # ASP.NET Core Web API — Clean Architecture, 4 layers. Talks to Postgres and to engine/.
engine/     # Separate .NET microservice wrapping the KataGo binary as a child process.
frontend/   # React + Vite SPA, feature-based structure.
docs/       # GOALS.md + architecture/decisions/ (ADRs).
scripts/    # Dev/infra shell scripts spanning all stacks.
./          # docker-compose.yml, .env.example, .editorconfig
```

`docker-compose.yml` brings up `postgres` and `engine`. The `backend` service references a `backend/Dockerfile` that **does not exist yet**, so `up backend` still fails to build. The frontend is not a compose service — run it locally.

## Status

The backend is playable but some features (auth, warmup, health checks) are incomplete. The engine is v1 complete and containerized. The frontend is an empty scaffold.

`ICurrentPlayer` is registered only under `IsDevelopment()`. The backend cannot run outside Development until auth ships.

Open work lives in GitHub issues and ADRs.

## Commands

Each stack's commands live in its own CLAUDE.md. Root-level only:

```
scripts/dev-up.sh           # docker compose up -d --build postgres engine — infra only for now
scripts/db-add-migration.sh # dotnet ef migrations add <Name> — generates a migration, does not apply it
scripts/db-migrate.sh       # dotnet ef database update
scripts/db-reset.sh         # down --volumes, re-up postgres, re-migrate — destroys local data
scripts/test-backend.sh     # --unit | --integration | --all (default), --coverage
scripts/test-engine.sh      # --unit | --integration | --all (default), --coverage
```

Both test scripts forward unrecognized arguments to `dotnet test`, and a `.csproj`/`.slnx` path argument overrides the stack's default solution.

## Environment variables

A template `.env.example` is at the repo root. ASP.NET Core convention: double underscores map to nested config sections.

Anything run **locally** rather than through compose needs these exported first, per shell. The test assemblies are the exception: they load `.env` themselves with `DotNetEnv`.

```bash
set -a && source .env && set +a
```

Machine-local or secret values (absolute paths, connection strings) come from `.env` via `IOptions<T>`, not from `appsettings.json`.

## .NET conventions

- Target **net10.0**; solutions are the XML `.slnx` format.
- Classes not built for inheritance are `sealed` by default unless something external requires it not to be. Exceptions: vendored `GoSharp`, EF Core migration files, unmodified template scaffolding.
- Use `sealed record` for immutable value-shaped types (DTOs, query/response objects), and a plain class with settable properties only where something external requires it (e.g. options types bound from configuration).
- Try-pattern (`TryMakeMove`, `TryRecordMove`, `TryUndo`) for operations that can legally fail without it being exceptional — returns `bool`, leaves state unchanged on failure.
- Vendored and wrapped libraries stay fully behind an adapter. Their types never reach a public surface.
- Config via `IOptions<T>` bound from `.env`'s double-underscore vars.

### Linting (StyleCop.Analyzers)

Wired in via `Directory.Build.props` (backend and engine) plus the root `.editorconfig`.

**Overrides:**

- No copyright header (`SA1633` off)
- `this.`-prefix not required (`SA1101` off)
- `_camelCase` required for private instance fields (`SX1309` on).
- Test projects get the analyzer but not doc-comment enforcement (`SA1600` silenced under `**/tests/**/*.cs`).
- EF migration bodies are marked `generated_code` in `.editorconfig`.
- `GoSharp` is exempt because `Directory.Build.props` never adds the analyzer to that project (not via `.editorconfig`).

Pinned to `1.2.0-beta.556`, not latest stable — stable predates proper positional-record support and false-positives on record parameters like `Coordinates(int X, int Y)`.

An `.editorconfig` section whose glob matches nothing fails silently — the build just keeps warning. `a/**/*.cs` does **not** match `a/foo.cs`; `**/` needs an intervening directory.

`dotnet build` skips recompiling (and re-warning on) projects whose inputs haven't changed — use `--no-incremental` to see every current warning.

## .NET testing conventions

Per-stack detail is in each stack's CLAUDE.md. What holds everywhere:

- Reach for hand-written fakes rather than a mocking framework as the first choice.
- Two tags: `Category` answers _should this run here_: `Integration` marks classes that leave the process, everything else is left untagged (`Category!=Integration` matches absent traits). `Requires` answers _what must be provisioned_: `Docker` (Testcontainers for Postgres), `Engine` (a running service at `Engine__BaseUrl`), `KataGo` (gitignored binary + models).
- Collections constrain concurrency: `"Postgres"` exists to _share_ one container fixture; `"KataGo"` exists to _serialize_ — each process is memory-hungry, and running two at once can OOM-kill them and cause flaky timeouts.
- Collections only serialize within one assembly. Running the backend and engine integration suites simultaneously still contends; so does leaving the engine container up while running engine integration tests.
