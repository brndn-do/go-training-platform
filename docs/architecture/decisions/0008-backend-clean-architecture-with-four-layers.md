# 8. Backend: Clean Architecture with four layers

Date: 2026-08-14

## Status

Accepted

## Context

The backend owns business rules (game legality, turn order, scoring outcomes, bot orchestration) that need to stay correct and testable independent of how they're persisted or exposed over HTTP.

## Decision

We will structure the backend as four layers with a strict dependency direction, each depending only on the layer(s) inside it:

- **Domain** — entities and business rules.
- **Application** — use-case orchestration, and the interfaces the outer layers implement. Depends only on Domain.
- **Infrastructure** — persistence, and the client to the engine service. Implements Application's interfaces.
- **Api** — the HTTP surface and dependency-injection wiring.

**Reasons:**

- Business rules need to stay correct and testable independent of how they're persisted or exposed over HTTP.

## Consequences

**Positive:**

- Domain logic is unit-testable without real databases or HTTP.
- Swapping the persistence technology, or the client to the engine, only touches Infrastructure. Domain and Application are unaffected.
- The dependency direction is enforced by the compiler: an inner layer referencing an outer one simply won't build.

**Negative:**

- This is more ceremony than a flat structure or layered architecture. Initial development may be slower but should pay off long-term, especially as the project grows.
