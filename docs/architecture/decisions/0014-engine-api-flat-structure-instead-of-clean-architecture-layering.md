# 14. Engine: flat structure instead of Clean Architecture layering

Date: 2026-08-14

## Status

Accepted

## Context

The backend uses a strict, four-layer Clean Architecture (ADR 8), justified by business rules that need to stay independent of persistence and transport. The engine microservice (ADR 2, ADR 9) has neither a rich domain model to protect nor a persistence layer. Its hard problems are process/IO plumbing (managing a child process, serializing concurrent callers, parsing a protocol) and a small amount of decision logic (which move to pick, how to build a query).

## Decision

We will structure the engine as a single project with three folders, not multiple projects:

- **Decision logic** — query construction, response interpretation
- **Process/IO plumbing** — owning and talking to the child KataGo process
- **HTTP layer**

**Reasons:**

- Clean Architecture's layering earns its cost by isolating domain rules from persistence and transport concerns — the engine has neither, so there's nothing for the extra structure to protect.
- With only three small, stable concerns, a full multi-project layering would add ceremony without adding much safety.

## Consequences

**Positive:**

- Much less ceremony for a service this size.

**Negative:**

- The folder boundaries are not compiler-enforced the way the backend's layer boundaries are.
- The HTTP layer is allowed to depend on the decision layer's concrete types directly.
