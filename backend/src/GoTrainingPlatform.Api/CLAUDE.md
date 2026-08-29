# CLAUDE.md — Api

The HTTP layer and the composition root. **Controllers**, not minimal API.

- `Controllers/GamesController.cs` — seven endpoints under `api/games`. It takes the action and maps the outcome.
- `Contracts/` — request/response DTOs, each with a static `From(...)` mapper.
- `ErrorHandling/GameExceptionHandler.cs` — `IExceptionHandler`.
- `CurrentPlayerOptions`/`DevelopmentCurrentPlayer`, and `Program.cs`.

## Response mapping

`OrchestrationResult` collapses to 404 (no such game, or not the current player's), 400 (rejected action, carrying no reason), or 200 — 201 plus `Location` from `Start`.

## Failure mapping

`GameExceptionHandler` maps what the application layer throws and declines anything it doesn't recognize, leaving the framework to report a generic 500.

| Thrown                                                                                                                | Status                |
| --------------------------------------------------------------------------------------------------------------------- | --------------------- |
| `RepositoryFailureKind.Conflict`                                                                                      | 409                   |
| `RepositoryFailureKind.Unavailable`, `EngineFailureKind.Unavailable`                                                  | 503                   |
| `RepositoryFailureKind.Rejected`, `EngineFailureKind.InvalidRequest`/`InvalidResponse`, `InvalidBotResponseException` | 500                   |
| `OperationCanceledException`                                                                                          | handled and swallowed |

- Exception messages go in the log, not the response body. `ProblemDetails.Detail` says what the client should do.
- The 409 says reload, not retry, and carries no game state.
- `app.UseExceptionHandler()` must stay first in the pipeline.

## Contract conventions

- Request DTOs use nullable properties with `[Required]`.
- Enums serialize as strings (`JsonStringEnumConverter`, registered in `Program.cs`).
- `GameResponse.Board` is jagged (`Content[][]`), indexed `[x][y]` to match the domain rather than flipping to row-major.
- Every response ships the full board.

## Composition root

`Program.cs` runs with `ValidateScopes` and `ValidateOnBuild`, so a missing or mis-scoped registration fails at `Build()`.

`ICurrentPlayer` is registered only under `IsDevelopment()`, so any other environment fails at startup. Deliberate, and stays until auth ships.

`DevelopmentCurrentPlayer`'s id has no matching row in the user table, and `games.player_id` is a foreign key onto it, so game creation fails in Development. Tests cover the game loop until real auth replaces this.

## Tests

`Api.Tests` uses `WebApplicationFactory` with the repository and engine faked, supplying its own configuration through `UseSetting`. It references `Application.Tests` to reuse the fakes rather than maintaining a second set.
