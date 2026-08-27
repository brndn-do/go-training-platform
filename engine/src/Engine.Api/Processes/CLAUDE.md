# CLAUDE.md — Processes

Owns the KataGo child process and everything that talks to it. Two singletons, both `IAsyncDisposable`:

- **`KataGoProcessIO`** — starts and owns the `katago` process; `ExchangeAsync` writes one line and reads one line back.
- **`KataGoClient`** — implements `Analysis/`'s `IKataGoClient`, serializing concurrent callers so only one query is ever in flight.

## `KataGoProcessIO`

- Deals only in raw strings.
- `RedirectStandardInput` must be set, or KataGo treats stdin EOF as "no more queries" and exits almost immediately.
- Redirecting stdout/stderr without continuously draining them is a hidden deadlock once KataGo's output exceeds the OS pipe buffer.
- KataGo's startup and diagnostic logging goes to stderr; the JSON protocol is on stdout. The readiness signal is the line `Started, ready to begin handling requests`, on stderr.

## `KataGoClient`

- Lets multiple callers share one stateful resource (the KataGo engine) without one caller's cancellation ever reaching into another caller's request.
- Each request goes into a shared queue (`Channel`), and each caller gets its own `TaskCompletionSource` for the result.

## Tests

`FakeKataGoProcessIO` covers `KataGoClient` without a real process, gating a response so cancellation and queueing can be tested deterministically.

`KataGoProcessIOIntegrationTests` launches the real binary and needs the `KataGoProcess__*` env vars. Model loading dominates the time.
