# CLAUDE.md — Endpoints

The HTTP surface. **Minimal API**, with route handlers grouped into `Map*` extension methods and not written inline in `Program.cs`.

```
GET  /health/startup   # KataGo has finished loading
GET  /health/ready     # Katago has loaded, has not exited, and is not stuck on a query past a threshold
GET  /health/live      # same as ready, but with a longer threshold
POST /suggestion       # { moves, boardSize, komi, botStrength } -> { move, blackWinRate }
POST /warmup           # Force KataGo load (if not loaded) without sending a suggestion request
```

## Error mapping

`Analysis/`'s exceptions become status codes here:

| Thrown                                                               | Status |
| -------------------------------------------------------------------- | ------ |
| `ArgumentException`/`ArgumentOutOfRangeException`                    | 400    |
| `InvalidKataGoResponseException`                                     | 500    |
| `InvalidOperationException`/`ObjectDisposedException` from `/warmup` | 500    |

`OperationCanceledException`, and any other uncaught exceptions, are **not** handled. Low-priority as Engine is internal, not public-facing.

## Health checks (Azure Container Apps, Kubernetes-style)

- **startup** — `HasLoaded`. Gates traffic until the slow model load finishes.
- **ready** — loaded, not exited, not stuck past a short threshold. Drains traffic from a wedged instance.
- **live** — the same predicate on a longer threshold. Restarts the container.

Thresholds are configurable (`ReadyHealthCheck`/`LiveHealthCheck` sections). Ready's should stay shorter than live's, so a wedged container gets drained before resorting to a restart.

**Note on `HasExited`:** a platform doesn't restart a container if a child process inside it crashed. If KataGo dies, the ASP.NET Core host keeps running fine. Without this signal a dead engine looks healthy forever.

## Tests

Endpoint tests use `FakeKataGoClient`, covering routing, binding, and status mapping without a real process. The health checks are tested directly against their `IKataGoClient` inputs, so each threshold boundary can be hit exactly.
