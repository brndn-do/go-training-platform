# 28. Identity user as the player

Date: 2026-08-26

## Status

Accepted

## Context

ADR 27 puts credentials in ASP.NET Core Identity, which brings its own user table. Identity's key is a `string` by default but is a type parameter, so it can be a `Guid`; it has to be settled in the initial migration, because changing it afterwards means dropping and re-creating Identity's tables.

The current schema is just `games` and `moves`. `games.player_id` is a bare `uuid` with no foreign key or table behind it, and nothing about a player is stored anywhere beyond that column.

ADR 26 enforces authorization in `GameService`, via an injected `ICurrentPlayer` that yields a `Guid`. Whatever is chosen here must supply a stable identifier to that seam, or change it.

We need to decide whether the authenticated user is the player, or whether a domain entity sits alongside it.

## Options considered

1. **The Identity user is the player.** `games.player_id` becomes a foreign key onto Identity's user table, whose key is set to `Guid`.
    - Pros: One row per person and one id. `ICurrentPlayer` reads that id from the authenticated user's claim, with no lookup or join. Registration stays a single insert.
    - Cons: A domain concept lives on a framework type in `Infrastructure`. Per-player state added later lands on the Identity user until a `Player` is extracted.

2. **A separate `Player` entity, joined one-to-one with the Identity user.** Sharing the Identity user's key would keep `ICurrentPlayer` a claim read, with no join.
    - Pros: The domain shape stays independent of the auth provider, and per-player state has somewhere to live that is not a framework type.
    - Cons: Two rows per person, which must be created together. A failure between them leaves a user who can sign in but owns no player, and every game action then fails on the foreign key. A second table and a second row for an app with no per-player domain state yet.

## Decision

We will treat the Identity user as the player. Identity's key type is set to `Guid`, and `games.player_id` becomes a foreign key onto Identity's user table.

**Reasons:**

- No per-player domain state exists, so a separate entity would be a second table and row carrying only an id.
- Registration stays a single insert. Two rows would have to be made atomic, a correctness hazard taken on for no present benefit.
- Identity's key can be a `Guid`, so `games.player_id` stays a `uuid` and ADR 26's seam is untouched.
- Extracting a `Player` later is additive: create the table, backfill from the user ids, re-point the foreign key. No row of `games` changes, and nothing above `Infrastructure`.

## Consequences

**Positive:**

- `games.player_id` gets a real foreign key, so orphaned games stop being representable.
- Registration is a single insert, with no partial-account state to guard against.
- `ICurrentPlayer` becomes a claim read with no database round trip. Its contract is unchanged, so ADR 26 needs no rework.

**Negative:**

- A domain concept lives on a framework type in `Infrastructure`, against ADR 8's dependency direction.
- The key must be set to `Guid` in the initial migration (ADR 27).
- Per-player state has no home but the Identity user. The cost of extracting grows with each field added there, so the time to extract is the first field that is not about authentication.
- `ApplicationUser` must never appear in an `Application` or `Domain` signature. `ICurrentPlayer`'s `Guid` stays the only way the domain learns who is acting; without that, this decision stops being reversible.
