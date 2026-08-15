# 11. Stateless backend: rebuild games from persisted state

Date: 2026-08-14

## Status

Accepted

## Context

A single game can span many separate requests over minutes, hours, or days. The backend runs as an HTTP API (ADR 4) backed by PostgreSQL (ADR 5) via EF Core (ADR 6), and may eventually run multiple instances. We have to decide whether to keep game state in memory across requests, or treat the database as the single source of truth — persisting state to the database after each request, discarding it from memory, and rebuilding it fresh on the next.

## Decision

We will not hold game state in memory across requests. Each request that needs a game loads its persisted move history from the database and rebuilds the game state in memory by replaying every move, then discards it once the request completes. This is a temporary choice to revisit if we expect the move-replay cost or multi-instance behavior to make this impractical.

**Reasons:**

- Matches a stateless HTTP API: any instance can serve any request without sticky sessions or a shared in-memory registry of active games.
- Avoids memory growth from holding multiple state across games and users.
- Keeps the database as the single source of truth, with no in-memory cache that could drift from it.
- Simpler to build for now; an in-memory cache or state store can be added later if replay cost becomes a problem.

## Consequences

**Positive:**

- The backend can scale horizontally with no sticky sessions or distributed cache — any instance can serve any request.
- Restarting the backend loses nothing, since no in-memory game state exists to lose.
- No cache-invalidation problem: the database is always authoritative.

**Negative:**

- Every request that touches a game replays its full move history instead of an O(1) in-memory lookup — a cost that grows with game length.
- Two requests for the same game (e.g. opened in different tabs) can each rebuild and save independently, with no in-memory state to catch the conflict. This is a race condition we'll need to consider.
- If this becomes a real performance problem (long games, high request volume), it will need revisiting.
