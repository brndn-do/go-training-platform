# 24. Package backend as a Docker container

Date: 2026-08-15

## Status

Accepted

Supersedes [19. Package backend as a Docker container](0019-package-backend-as-a-docker-container.md)

## Context

ADR 19 rejected containerizing the backend: no genuine technical need, and the CI/CD-uniformity and portability arguments in favor were too modest to outweigh the added operational overhead.

ADR 23 needs the engine reachable only by the backend. Leaving the backend and engine on separate hosting products to achieve that means paid cross-product infrastructure. If the backend were a container too, it could run on the same platform as the engine, where that isolation comes free.

Options considered:

- **Leave the backend non-containerized (status quo per ADR 19)** — accept whatever cross-product networking cost ADR 23's requirement carries.
- **Containerize the backend now**, given the new concrete motivation.

## Decision

We will package the backend as a container.

**Reasons:**

- Enables running the backend on the same platform as the engine, which allows us to achieve the engine's isolation for free (ADR 23).
- The original objection — no concrete benefit beyond consistency — no longer holds.
- Retains ADR 19's more modest benefits.

## Consequences

**Positive:**

- Deployable to any container host, not tied to one platform's code-deploy mechanism.
- Runtime pinned exactly, per ADR 19.

**Negative:**

- New operational overhead (Dockerfile, a registry, a build/push step) which is worth paying now.
- Whether this also gives up platform-managed patching depends on where the container is hosted.
