# 5. PostgreSQL as the Database

Date: 2026-08-14

## Status

Accepted

## Context

The backend needs durable, relational storage for games and their move histories, with move records that stay consistent with game state. Users and authentication are planned but not yet built; once they exist, games will belong to users the same way moves belong to games. We're also using ASP.NET Core (ADR 4), which narrows this to databases with solid EF Core support.

## Decision

We will use **PostgreSQL**, running in a container locally. We expect to run a managed instance in production, though that hosting decision isn't finalized.

**Reasons:**

- Strong relational integrity fits the games/moves model directly, and the planned users relationship the same way.
- No licensing cost, and runs identically across any OS via containers.
- Mature EF Core provider, with room to grow into (JSONB, full-text search).

## Consequences

**Positive:**

- Foreign keys and transactions enforce integrity at the database level.
- Same environment in dev and prod.

**Negative:**

- Schema changes go through migrations, which adds a bit of ceremony.
- A few Postgres-specific features may get used where convenient, which would mean rework if we ever switched databases.