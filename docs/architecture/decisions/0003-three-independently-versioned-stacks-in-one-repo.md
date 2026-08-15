# 3. Three independently-versioned stacks in one repo

Date: 2026-08-14

## Status

Accepted

## Context

This project has three different runtime concerns: a backend API (business logic and persistence), an engine service (a CPU-bound wrapper around KataGo, with its own scaling and cold-start profile), and a frontend (served statically). Each has its own dependency graph and build tooling, and the engine in particular needs to scale on CPU load and cold-start behavior independently of the backend's request volume.

## Decision

We will keep the backend, engine, and frontend as three independently-versioned stacks inside one repository, each with its own build/test tooling and commands. Only shared config lives at the repo root.

**Reasons:**

- The three stacks are different runtime concerns with different dependency graphs and build tooling.
- The engine specifically needs to scale on CPU load and cold-start behavior, independently of the backend's request volume.

## Consequences

**Positive:**

- Each stack can be built, tested, and reasoned about independently.
- The engine can be deployed and scaled as its own service, separately from the backend, matching its different (CPU-bound, cold-start-sensitive) profile.

**Negative:**

- Cross-stack changes, most notably anything touching how the backend and engine communicate, require coordinating two independent projects rather than one, and there's no compiler-enforced check that the backend's expectations of the engine's contract stay in sync. That has to be caught by integration testing or discipline.
- The shared root-level infrastructure config needs to be kept current as each stack's own configuration evolves.
