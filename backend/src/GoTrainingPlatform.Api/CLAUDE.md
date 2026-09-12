# CLAUDE.md — Api

The HTTP layer and the composition root. **Controllers**, not minimal API.

- `Controllers/GamesController.cs` — the game endpoints under `api/games`. It takes the action and maps the outcome.
- `Controllers/AuthController.cs` — register, login, and logout under `api/auth`, over Identity's `UserManager` and `SignInManager`.
- `Contracts/` — request/response DTOs, each with a static `From(...)` mapper.
- `ErrorHandling/GameExceptionHandler.cs` — `IExceptionHandler`.
- `HttpContextCurrentPlayer`, and `Program.cs`.

## Response mapping

`OrchestrationResult` collapses to 404 (no such game, or not the current player's), 400 (rejected action, carrying no reason), or 200 — 201 plus `Location` from `Start`.

`AuthController` maps Identity's results, which are returned rather than thrown. Register's failures become a 400 `ValidationProblemDetails` keyed by Identity's error codes. Every failed login becomes the same 401, so a response never says whether an account exists.

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

`ICurrentPlayer` is `HttpContextCurrentPlayer`, registered scoped, reading the user id claim from the login cookie. It throws when nobody is signed in, which `MapControllers().RequireAuthorization()` rules out for any action not marked `[AllowAnonymous]`.

`UseStatusCodePages()` gives bodiless error responses a `ProblemDetails` body, including the 401 for a request without a session.

## Tests

- `GamesEndpointsTests` uses `WebApplicationFactory` with the repository and engine faked, supplying its own configuration through `UseSetting`. `TestAuthHandler` signs every request in as one user per host.
- `PostgresApiFixture`, shared by the `"PostgresApi"` collection, runs the real host against Testcontainers Postgres. Its clients use an `https://` base address, because the session cookie is `Secure` and would not be sent back over `http`.
- A test that a request without a session gets 401 belongs on the real host. A test authentication scheme answers 401 by itself, so it can't catch a broken cookie configuration.

`Api.Tests` references `Application.Tests` to reuse its fakes rather than maintaining a second set.
