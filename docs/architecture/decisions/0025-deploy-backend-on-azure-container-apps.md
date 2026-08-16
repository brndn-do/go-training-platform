# 25. Deploy backend on Azure Container Apps

Date: 2026-08-15

## Status

Accepted

Supersedes [20. Deploy backend on Azure App Service](0020-deploy-backend-on-azure-app-service.md)

## Context

ADR 20 chose Azure App Service, partly for the F1 free tier's fit and to match the project's non-container local-dev workflow. The backend is now containerized instead (ADR 24), specifically to enable running it on the same platform as the engine.

Azure Container Apps gives apps in the same environment free, built-in internal DNS, and an app with internal-only ingress is reachable only from within that environment (ADR 23).

Options considered:

- **Run the containerized backend on App Service anyway** — keeps App Service's managed patching, but doesn't solve ADR 23's isolation requirement.
- **Run the backend on Azure Container Apps, in the same environment as the engine** — solves ADR 23's requirement for free; keeps both services in one deployment model.

## Decision

We will deploy the backend on Azure Container Apps, in the same environment as the engine.

**Reasons:**

- Solves ADR 23's isolation requirement without paid infrastructure.
- Standardizes the backend and engine onto one deployment model and CI/CD pipeline shape.

## Consequences

**Positive:**

- Solves ADR 23's isolation requirement without any additional paid Azure infrastructure.
- One deployment mechanism and CI/CD pipeline shape for both services.
- The backend gains Container Apps' scale-to-zero economics, same as the engine.
- `docker compose` can bring up the backend, engine, and Postgres together locally.

**Negative:**

- Loses App Service's platform-managed OS/runtime patching.
- Loses the F1 tier's unconditional free pricing for Container Apps' consumption billing — likely still near-zero here, but a different model.
- Ties the backend's and engine's hosting decisions together more tightly — moving just one of them later becomes a big decision.
