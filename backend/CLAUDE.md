# CLAUDE.md — backend

ASP.NET Core Web API, Clean Architecture. Each project carries its own file details:

| Project              | File                                                                                               |
| -------------------- | -------------------------------------------------------------------------------------------------- |
| `Domain`             | [src/GoTrainingPlatform.Domain/CLAUDE.md](src/GoTrainingPlatform.Domain/CLAUDE.md)                 |
| `Application`        | [src/GoTrainingPlatform.Application/CLAUDE.md](src/GoTrainingPlatform.Application/CLAUDE.md)       |
| `Infrastructure`     | [src/GoTrainingPlatform.Infrastructure/CLAUDE.md](src/GoTrainingPlatform.Infrastructure/CLAUDE.md) |
| `Api`                | [src/GoTrainingPlatform.Api/CLAUDE.md](src/GoTrainingPlatform.Api/CLAUDE.md)                       |
| `GoSharp` (vendored) | [src/GoSharp/CLAUDE.md](src/GoSharp/CLAUDE.md)                                                     |

## Layers

Strict dependency direction; inner layers never reference outer ones.

```
Domain  <-  Application  <-  Infrastructure  <-  Api
   ^
GoSharp (vendored)
```

## Commands

```
dotnet build                    # nine projects (five src, four test)
dotnet build --no-incremental   # see all current warnings reliably
dotnet test                     # all four test projects
dotnet run --project src/GoTrainingPlatform.Api
```

From the repo root, `scripts/test-backend.sh --unit` skips everything that leaves the process; `--integration` runs only those 20.

To pass the full suite: Docker running (Testcontainers), plus a running engine container for `EngineClientIntegrationTests` — its `Engine__BaseUrl` comes from the repo-root `.env`, loaded by the test assembly itself using `DotNetEnv`. Without the engine, four tests fail. A container built before an engine change still serves the old contract — rebuild with `docker compose build engine`.

## Request flow

`GamesController` → `TurnOrchestrator` → `GameService` → `Game`. Rules live at the innermost end of that chain and nowhere else.

- `TurnOrchestrator` decides whether the bot moves next, and sequences human → bot → hint synchronously inside one request. No message bus, no event system.
- `GameService` delegates every rule check to `Game`'s `Try*` methods and persists only on success.
- `GET /api/games/{id}` is the one route that skips the orchestrator; `POST {id}/resume` is the read that does advance the bot.

## Invariants

- No game state is held between requests. Every request reloads the move history and replays it.
- Concurrency is `xmin`-based: a lost race throws `DbUpdateConcurrencyException` → `RepositoryFailureKind.Conflict` → 409, which means reload, not retry.
- Game ownership is enforced in the application layer's `GameService` — not in the controller, not in the repository.

## Tests

One xUnit project per layer. `Api.Tests` references `Application.Tests` to reuse its fakes.
