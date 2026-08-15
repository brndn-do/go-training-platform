# 18. Package engine as a Docker container

Date: 2026-08-15

## Status

Accepted

## Context

The engine needs to run as a long-lived server process, in a way that behaves consistently across local development and production, and that isn't tied to the specific configuration of whatever machine happens to host it. It has environment dependencies a plain code deployment wouldn't naturally carry along with it — a native KataGo binary built for a specific CPU instruction set, plus a specific threading configuration (ADR 15) — that need to match exactly wherever it runs.

We can install the engine's runtime and dependencies directly onto whatever host runs it (a VM, or a PaaS "code deployment" model), or package it as a container image that bundles its own runtime and dependencies together.

## Decision

We will package the engine as a Docker container.

**Reasons:**

- Portability: a container image runs identically on any container-capable host, decoupling how we package the engine from where it eventually runs.
- Predictable deployments: a container image pins the exact runtime environment (library versions, OS layer) at build time.

## Consequences

**Positive:**

- The engine's environment dependencies (a specific native binary, specific CPU/threading assumptions from ADR 15) are locked into the image at build time.
- Not tied to any specific hosting platform.

**Negative:**

- Build/packaging overhead: a Dockerfile, a container registry, and a build/push step become part of the pipeline instead of a direct publish.
- Image size is a concern given the engine's large model files.
