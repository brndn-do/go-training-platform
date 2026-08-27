# CLAUDE.md — Application

Use cases, plus the interfaces the outer layers implement. Depends only on `Domain`.

- `Games/` — `GameService`, `IGameRepository`, `GameActionResult`, `RepositoryException`/`RepositoryFailureKind`.
- `Orchestration/` — `TurnOrchestrator`, `IEngineClient`, `EngineSuggestion`, `OrchestrationResult`, `EngineException`/`EngineFailureKind`, `InvalidBotResponseException`.
- `ICurrentPlayer` and `Actor` sit at the project root, being cross-cutting rather than belonging to either folder.

## The two services

`GameService` holds no rules of its own — it loads, delegates to `Game`'s `Try*` methods, and persists only on success.

`TurnOrchestrator` is the only thing that decides whether the bot should move. `GameService` never does.

## Rules that live here

- Ownership is enforced in `GameService`, right after the load and before any write. A game that doesn't exist and a game a user does not own are treated the same way with `null`.
- A bot move the domain rejects throws `InvalidBotResponseException` — the engine returning an illegal move is a real failure.

## Interfaces and failures

`IGameRepository` and `IEngineClient` are defined here and implemented by `Infrastructure`.

`RepositoryException` and `EngineException` each carry a `Kind` enum and no type from the underlying store or transport. Add a `Kind` value for new failure modes; don't leak a provider exception outward.

## Tests

Hand-written fakes: `FakeGameRepository`, `FakeEngineClient`, `FakeCurrentPlayer`.

Asserts `FakeGameRepository.SaveAsyncCallCount` to test that a write happened.

This project is referenced by `Api.Tests`, which reuses these fakes. Changing one affects both suites.
