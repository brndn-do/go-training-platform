# 20. Deploy backend on Azure App Service

Date: 2026-08-15

## Status

Accepted

## Context

The backend was decided against containerizing (ADR 19, rejected). That leaves the question of what actually hosts it. It's a standard ASP.NET Core Web API with no exotic dependencies, and the project's own existing local-dev convention already runs it via plain `dotnet run`, not Docker.

## Decision

We will deploy the backend on Azure App Service, using its direct code-deployment model rather than packaging it as a container (App Service supports both; we're deliberately using the non-container path).

**Reasons:**

- No technical need for container-level environment control (ADR 19) — App Service's managed .NET runtime is a natural fit for a standard ASP.NET Core Web API.
- Matches the project's own existing local-dev convention. App Service's code-deploy model is the same basic shape, just hosted.
- Since .NET is a Microsoft-developed runtime, App Service has first-class, day-one support for it — native Visual Studio/VS Code publish integration, deployment slots built around the .NET app lifecycle, and new .NET versions land there without waiting on a third party to catch up.
- App Service's F1 (free) tier fits where this project actually is right now — no cost while there's no real production traffic — and upgrading later is a pricing-tier change (e.g. to B1/S1), not a different deployment mechanism or a re-architecture.

## Consequences

**Positive:**

- Platform-managed OS/runtime patching — Microsoft keeps the underlying environment current, not us.
- Simpler, faster deploy path: publish compiled output directly, no image build/push cycle.
- Less operational surface to learn and maintain for this specific service.
- Access to a free tier.

**Negative:**

- Ties the backend's deployment mechanism specifically to Azure App Service — migrating to a different cloud or platform later means a real deployment-mechanism change, not just repointing a container at a new host.
- The backend and engine now have two different deployment mechanisms (see ADR 19).
- Once an orchestration platform is chosen for the engine, the backend won't automatically inherit whatever health-probe or scaling patterns get built for it — it has its own separate model under App Service.
- F1 specifically has limits: no "Always On", a small CPU/memory quota, and shared multi-tenant infrastructure. Fine for now; not a tier to still be on once this has real users.
