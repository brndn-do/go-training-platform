# 13. Optimistic concurrency for Game via Postgres xmin

Date: 2026-08-14

## Status

Accepted

## Context

A single game can, in principle, be mutated by two overlapping requests. Most concretely, an orchestrated turn's human-move save and its bot-move save (ADR 12) are two independent commits within one request. This opens a narrow window between them where a second request could read stale state, save its own change, and have that change silently overwritten by the first request's second commit. The core game aggregate isn't a shared, long-lived object — each request rebuilds its own copy from persisted state, so nothing stops two independently-loaded copies from being saved over each other (e.g. two different devices loading the same game).

## Decision

We will use Postgres's `xmin` system column as a row-version check on the game aggregate. A concurrent save that loses this check throws a concurrency exception instead of silently overwriting another request's write.

**Reasons:**

- Without this, two overlapping requests can silently overwrite each other's writes.
- The game aggregate isn't a shared, long-lived object — it's rebuilt fresh per request, so nothing in memory prevents this on its own.

## Consequences

**Positive:**

- No extra schema or migration needed as `xmin` is a system column Postgres already maintains on every row.
- No locking: concurrent requests can both read and attempt to save without blocking each other, and only the loser pays the cost.
- The failure mode is explicit: a concurrency exception the caller must handle, instead of a silent overwrite.

**Negative:**

- Every write path for the game aggregate now needs to handle the concurrency exception (retry, surface an error, etc.).