# 26. Authorization enforced in the application layer

Date: 2026-08-22

## Status

Accepted

## Context

We plan on adding authentication to our app, with games belonging to an authenticated user. Naturally, we want authorization logic for game actions, so that users can only interact with games they own.

Ownership can only be checked with the game in hand, and `GameService`'s game-action methods load, mutate, and persist a game in a single call — so where the check lives also decides whether it can run before the write.

We need to decide where authorization for game actions live.

## Options considered

1. **API Layer:** The controller does a pre-check before calling the application layer by loading the game and comparing owners.
    - Pros: Our existing application layer stays untouched, the check sits next to the claim it reads
    - Cons: Checking the returned result is too late — `GameService.MakeMoveAsync` has already persisted the move by then — so the check has to be a pre-check, costing an extra database trip. Decision rules sit outside the application layer.

2. **ASP.NET Core's `IAuthorizationService`:** The framework's own resource-based authorization — a requirement, a handler, and a policy, given the loaded game.
    - Pros: The framework's recommended pattern for resource-based authorization, composes well once there are several rules
    - Cons: In the API layer it has the same timing problem as option 1. In the application layer it needs a `ClaimsPrincipal`, which means referencing `Microsoft.AspNetCore.Authorization` from a layer that today depends only on Domain (ADR 8). Either way, a requirement, a handler, and a policy registration around a single Guid comparison.

3. **Application Layer**: Our application code checks for ownership and any other authorization logic we may add.
    - Pros: The game is already loaded at the point of the check, so it runs before any write and costs no extra round trip. Decision rules sit in the application layer, changes to authorization logic means changing our application code.
    - Cons: Requires changing our existing application code and tests. The application layer takes on a caller-identity concept it does not have today.

4. **Infrastructure Layer**: The repository doesn't return a game you don't own, and a game you don't own may as well not exist.
    - Pros: A single choke point no caller can bypass, since no code path loads a game unscoped
    - Cons: The repository now enforces an authorization rule, invisible to the use case relying on it. Switching persistence means reimplementing the rule.

## Decision

We will enforce authorization at the application layer, in `GameService`, immediately after it loads a game and before it writes.

The caller's identity will reach the application layer through an `ICurrentPlayer` abstraction —  defined in the application layer and implemented by the API layer, which reads the authenticated user's id.

**Reasons:**
  - Authorization is a policy decision that requires reasoning about the relationship between the user and a resource, and the resource is only in hand inside `GameService`.
  - `GameService` loads, mutates, and persists in one call, so a check anywhere else either runs after the write or pays for a second load.
  - Injecting the current player, rather than passing it as a parameter, keeps the bot's turn honest — the orchestrator drives the bot through the same `GameService` methods, and would otherwise have to pass the owner's id while the bot is the one acting.

## Consequences

**Positive:**
  - No caller can reach a write without passing the check.
  - No extra database trip just for authorization.

**Negative:**
  - Will require changing application code and tests we have already built.
  - The application layer now carries a caller-identity concept.
  - `GameService` can no longer be used outside a request without supplying an `ICurrentPlayer`.
  - Until authentication exists, every game belongs to the same fixed development player, so the check always passes when exercised by hand — unit tests are the only thing guarding it.
