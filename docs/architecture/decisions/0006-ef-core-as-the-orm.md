# 6. EF Core as the ORM

Date: 2026-08-14

## Status

Accepted

## Context

The backend needs a way to map games, moves, and (eventually) users onto PostgreSQL tables without hand-writing SQL for every query. We're using ASP.NET Core (ADR 4) and PostgreSQL (ADR 5), and want something that integrates cleanly with both — handling schema migrations as the data model evolves, and supporting the relational queries the move history needs.

## Decision

We will use Entity Framework Core as the ORM, with the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

**Reasons:**

- First-party ASP.NET Core integration.
- Migrations give a versioned, code-first way to evolve the schema as users/auth get added later.
- Mature Postgres provider with support for the JSONB and full-text features we may grow into.

## Consequences

**Positive:**

- Schema changes are tracked as code (migrations) instead of manual SQL scripts.
- Querying games/moves in C# with LINQ instead of hand-written SQL for most cases.

**Negative:**

- An object-relational mapper by nature hides some of the underlying SQL; this project accepts that abstraction cost in exchange for less hand-written data-access code.
