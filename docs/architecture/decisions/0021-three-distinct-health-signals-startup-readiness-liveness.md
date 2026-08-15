# 21. Three distinct health signals: startup, readiness, liveness

Date: 2026-08-15

## Status

Accepted

## Context

The engine's `/health` endpoint currently exposes a single, generic health check with nothing registered — always healthy, unable to distinguish different kinds of problems. The failure modes for the KataGo process wrapper fall into three different categories:

1. KataGo never finishes loading
2. KataGo is stuck/unresponsive mid-query after having already started
3. KataGo crashes/exits. Each of these needs a different response from 

We'd like to have:

- A generous one-time timeout before concluding startup failed (#1)
- An immediate "stop sending traffic here" when the process is unhealthy (#2 and #3)
- A more conservative check to trigger a restart once a problem is confirmed (#2 and #3)

## Decision

We will build three distinct health signals: startup, readiness, and liveness (Kubernetes-style probes), each answering a different question and triggering a different response, rather than one generic health check.

**Reasons:**

- The three real failure modes need different responses, which a single healthy/unhealthy signal can't express.
- Readiness and liveness need to react at different speeds to the same underlying problem; readiness reacts immediately to stop compounding harm, liveness waits for repeated failures before committing to a restart.

## Consequences

**Positive:**

- Each probe's failure gets the response that actually fits what went wrong, instead of just "restart everything" or "do nothing."

**Negative:**

- Three things to build, test, and keep correct instead of one.
- The exact mapping from raw signals to each probe's answer is nuanced and can be easy to get wrong.
