# 9. Engine as a separate, independently-deployable microservice

Date: 2026-08-14

## Status

Accepted

## Context

KataGo analysis (see ADR 2) is CPU-heavy and process-bound, and has a different scaling and cold-start profile than the rest of the backend: it needs to load two neural network models before it can serve a single query.

## Decision

We will build the engine as its own service, wrapping the KataGo binary as a child process and exposing it over a small HTTP API. We will deploy and scale it independently of the backend (see ADR 3), targeting a scale-to-zero platform.

**Reasons:**

- KataGo's CPU-heavy, process-bound, cold-start profile is fundamentally different from the rest of the backend.
- Bundling it into the backend API process would tie the backend's own scaling and deployment to the engine's process-management needs.

## Consequences

**Positive:**

- The backend can scale on request volume while the engine scales independently on CPU load. The two will be decoupled operationally, not just in code.
- Process-management concerns (starting/stopping the analysis binary, draining its output streams, warm-up, readiness/liveness) are entirely the engine's problem and never leak into the backend's own code.

**Negative:**

- The backend needs its own client to talk to the engine over the network, instead of an in-process method call. This brings certain costs (network latency, serialization, a failure mode where the engine is unreachable).
