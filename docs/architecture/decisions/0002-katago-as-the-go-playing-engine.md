# 2. KataGo as the Go-playing engine

Date: 2026-08-14

## Status

Accepted

## Context

The product needs bots across a wide strength range — from a near-unbeatable top tier to weaker, human-like play for beginners — plus a live move-suggestion/hint feature, all backed by a Go engine rather than one built from scratch. That engine needs to be queryable per-position (not just capable of self-play), and needs a way to produce plausible, human-like weaker play, not only its own strongest move.

## Decision

We will use KataGo as the underlying Go-playing engine, driven through its JSON analysis-engine protocol rather than its GTP (text-based, interactive-game-focused) protocol, and run as a wrapped child process.

**Reasons:**

- Need a strong engine across a wide strength range without building move-selection logic from scratch.
- Need independent, per-position queries, not just self-play — a fit for the analysis-engine protocol, not GTP.
- Need a way to produce plausible, human-like weaker play, not only the engine's own strongest move.

## Consequences

**Positive:**

- Access to a strong, actively-researched open-source engine instead of building move-selection logic from scratch — a problem far outside this project's scope.
- KataGo's analysis-engine protocol is built for independent, per-position queries with rich output (move probabilities, win-rate estimates).
- KataGo separately supports a human-supervised-learning model, which is what makes the weaker, human-like bot tiers possible.

**Negative:**

- The project is now dependent on KataGo's own protocol, configuration surface, and quirks.
- Running KataGo has real resource costs (CPU, memory, model load time) and needs careful decisions about how it's deployed and configured.
