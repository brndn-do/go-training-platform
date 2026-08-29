# CLAUDE.md — Infrastructure

Implements `Application`'s interfaces: EF Core behind `IGameRepository`, HTTP behind `IEngineClient`.

- `GoTrainingPlatformDbContext`, `GameConfiguration`, `GameRepository`, `Migrations/`.
- `EngineClient`, `EngineOptions`, and `Engine/` — the engine's wire DTOs, kept **`internal`**. Translate to `Application` types at the boundary.

## EF Core conventions

- Schema is **snake_case** via `EFCore.NamingConventions`.
- `Game` is the aggregate; `Moves` is an owned collection mapped to its own `moves` table, keyed `(GameId, MoveNumber)` with `ValueGeneratedNever()`.
- Concurrency is the `xmin` shadow property plus `IsRowVersion()`.
- `GameRepository.SaveAsync` diffs the owned collection by hand and mutates the tracked collection — see the lessons below.
- Both load paths go through `LoadAsync`, which checks `context.Games.Local` before querying.
- Migrations run with **Api** as the EF startup project (`scripts/db-add-migration.sh` to generate, `scripts/db-migrate.sh` to apply), so both need `ASPNETCORE_ENVIRONMENT=Development` until #24. Otherwise `ValidateOnBuild` fails on `ICurrentPlayer`, and EF swallows that into a misleading `DbContextOptions` error rather than naming the cause.

## Failure translation

`GameRepository.Translate` turns store failures into `RepositoryException` and returns `null` for anything else, so the exception filter leaves programming errors and cancellations alone. **Its ordering is load-bearing** — the cancellation check must stay ahead of the unwrap, and the unwrap must look at both the outer and inner exception. Both are commented at the site; read them before reordering.

## Tests

`Infrastructure.Tests` runs against a real Postgres from Testcontainers, migrated with the real EF Core migrations — no in-memory provider.

- `PostgresFixture` is shared across the `"Postgres"` collection and hands out a **fresh** `DbContext` per call.
- It also builds deliberately-broken contexts (`CreateUnreachableContext`, `CreateMissingDatabaseContext`) covering both sides of the transient/permanent split.
- `EngineClientIntegrationTests` needs a running engine; `Engine__BaseUrl` comes from the repo-root `.env`, which the test assembly loads itself. Without the engine, four tests fail, and the message distinguishes unreachable from running-but-not-ready.

## Lessons likely to recur

`GameConfiguration`/`GoTrainingPlatformDbContext` are the working example to copy from.

- EF Core can **never** constructor-bind a navigation property — a collection or an owned reference — however the constructor is shaped. Navigations are filled in after construction, so a rich-constructor entity still needs an all-scalar path available to it.
- A **get-only property needs an explicit `builder.Property(...)`** before convention-based discovery finds it, even when a constructor parameter matches its name and type exactly.
- **Explicit configuration bypasses naming conventions entirely** (e.g. `ToTable(...)`), so one stray mis-cased table can slip into an otherwise-consistent schema. Composite key members aren't exempt from value-generation conventions either.
- **`CurrentValues.SetValues(...)` reconciles scalars and owned _references_ but silently skips owned _collections_.** Those need diffing and mutating the tracked collection directly.
- **A model compiling, or a migration generating, proves nothing about materialization.** Check with a real `dotnet ef database update` or a load-and-check round trip — and use a fresh `DbContext` for the checking read, since a reused one's identity map hides real persistence bugs behind a passing test.
