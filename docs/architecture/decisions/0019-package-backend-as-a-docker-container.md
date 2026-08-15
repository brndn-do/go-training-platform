# 19. Package backend as a Docker container

Date: 2026-08-15

## Status

Rejected

Every argument in favor turned out to be either inherently modest (portability, runtime pinning) or smaller than first presented once actually examined: integration testing doesn't need the backend containerized, and the CI/CD-uniformity benefit is really just one different deploy step, not two full pipelines. Meanwhile every argument against held up: no genuine technical need and more operational overhead. See a follow-up ADR for the backend's actual deployment approach.

## Context

The engine is already being containerized (ADR 18), driven by a technical need: a native binary with specific CPU/threading tuning that has to be pinned exactly. On the other hand, the backend has no equivalent need — it's a standard ASP.NET Core Web API with no native dependencies.

The question is whether to do it anyway, for consistency with the engine and uniformity, or to use a non-container deployment model (e.g. Azure App Service) that fits the backend's simpler needs without the extra machinery.

## Decision

We will package the backend as a container too, matching the engine.

**Reasons:**

- Once an orchestration platform is chosen, having both services as containers means one deployment model, one set of health-probe patterns, and one CI/CD pipeline shape — rather than operating two different deployment mechanisms side by side indefinitely.
- Portability: not locked into one vendor's PaaS product, same reasoning as the engine.
- A container still pins the exact runtime environment (specific .NET version, package versions, OS layer).

## Consequences

**Positive:**

- One CI/CD pipeline shape reused for both services instead of two different ones.
- Not locked into a specific cloud vendor's PaaS product for the backend either.
- Local dev can bring up backend, engine, and Postgres together via `docker compose`.

**Negative:**

- The backend has no actual technical need for container-level environment control — unlike the engine, this is driven by consistency and uniformity, not necessity.
- Real, new operational overhead for a service that wouldn't otherwise need it: a Dockerfile, a registry, a build/push step.
- Gives up the platform-managed OS/runtime patching a code-deploy PaaS model would provide for free; keeping the base image current becomes an ongoing responsibility we own.
