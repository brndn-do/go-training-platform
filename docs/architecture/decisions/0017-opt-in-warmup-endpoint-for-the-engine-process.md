# 17. Opt-in warmup endpoint for the engine process

Date: 2026-08-14

## Status

Accepted

## Context

Constructing the engine's process wrapper starts the KataGo binary and loads two neural network models, which is a startup cost that takes noticeable time.

On a scale-to-zero deployment with zero active instances, whichever request happens to be the first `/suggestion` call pays that full cold-start cost, and the user behind it feels the delay directly.

We want a way for a caller who has good reason to expect a call to `/suggestion` is coming soon (e.g. the backend predicting a user is about to start or resume a game) to trigger that startup ahead of time.

We can create a dedicated endpoint to force-start an instance with its underlying KataGo process. An alternative would be having the caller send a throwaway query through the normal `/suggestion` path instead, and just discard the response.

## Decision

We will provide an optional `POST /warmup` endpoint that triggers construction of the process wrapper and the full model load without requiring an actual analysis query. It exists specifically to trigger start-up.

**Reasons:**

- On scale-to-zero, a `/suggestion` query can be the first thing to trigger construction, paying the full cold-start; a caller who can predict demand ahead of time should be able to absorb that cost before a real user is waiting on it.
- A dedicated endpoint doesn't need a query shape at all.
- A dedicated endpoint would allow us to avoid sending an actual query to the analysis engine, which could potentially compete with another query.

## Consequences

**Positive:**

- Whoever calls it gets to shave cold-start latency off without changing how normal queries behave.
- A caller with no reason to expect near-term demand can simply not call it.

**Negative:**

- One more endpoint to maintain and keep behaviorally correct.
- Relies on the caller to actually know when to use it and choose to. If nobody calls it, a cold instance is no better off than before.
