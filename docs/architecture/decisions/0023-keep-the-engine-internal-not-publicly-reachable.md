# 23. Keep the engine internal, not publicly reachable

Date: 2026-08-15

## Status

Accepted

## Context

The engine (ADR 9) runs as a separate microservice. Its HTTP surface — `/warmup`, `/suggestion`, and three health probes — has no authentication, so any caller that can reach it over the network can invoke it.

A publicly exposed engine is also, on its own, a security surface we'd need to carefully configure and maintain.

The backend runs on Azure App Service (ADR 20) and the engine on Azure Container Apps (ADR 22), which are two different hosting products. We need to decide whether to restrict the engine to backend-only traffic, and if so, how to do it with minimal added cost and complexity.

### Options considered

**1. Internal-only via VNet integration**
VNet-integrate the App Service backend, and place the engine's Container Apps environment in the same VNet, restricted to internal traffic.
- **Pros:** no public endpoint on the engine.
- **Cons:** more infrastructure to set up and keep correctly configured across two hosting products.
- **Cost:** VNet Integration is free, but requires upgrading off the Free/Shared App Service tier.

**2. Public ingress with a shared secret**
The engine checks a header/API key known only to the backend.
- **Pros:** simple to implement: one header check.
- **Cons:** weaker than network isolation. The secret must be generated, stored, and rotated. Engine is still publicly reachable.
- **Cost:** free.

**3. Public ingress, no authentication**
- **Pros:** nothing to build.
- **Cons:** open to abuse: anyone could spam `/suggestion` and run up billing.
- **Cost:** free to set up, but the abuse risk is a real cost.

**4. Public ingress with Managed Identity / Azure AD token validation**
The backend authenticates using its Azure-issued identity instead of a manual secret.
- **Pros:** no secret to manage — Azure issues and rotates tokens automatically.
- **Cons:** the least familiar of these five options to build and debug (requires JWT-bearer auth middleware in the engine). Engine is still publicly reachable.
- **Cost:** free.

**5. Move the backend onto Azure Container Apps, alongside the engine**
ACA gives apps in the same environment free internal DNS, and internal-only ingress limits reachability to that environment. Moving the backend there and setting the engine to internal-only removes the isolation problem entirely.
- **Pros:** no public endpoint on the engine — nothing to misconfigure or forget. No VNet, NAT Gateway, or paid App Service tier needed.
- **Cons:** requires containerizing the backend and deploying via ACA, reversing the reasoning in ADR 19 and ADR 20.
- **Cost:** trades App Service's managed patching and free F1 tier for Container Apps' consumption billing (likely near-zero at this scale, but a different model).

These options aren't mutually exclusive — e.g., internal-only ingress as the primary boundary, with Managed Identity as a second layer.

## Decision

We will keep the engine internal by containerizing the backend and deploy it on Azure Container Apps, in the same environment as the engine, then set the engine's ingress to internal-only within that shared environment. The engine is not exposed to the public internet and is reachable only from the backend, via the environment's built-in internal DNS. No authentication is added to the engine's own HTTP surface — the network boundary is the trust boundary.

**Reasons:**

- Of the two options that achieve a true internal-only boundary, this one gets there without any paid infrastructure — VNet Integration bridging App Service and Container Apps would require upgrading off the F1 free tier and configuring cross-product networking, while having both services on Container Apps gets the same boundary for free.
- The backend is the engine's only real consumer for now.
- Avoids designing and maintaining authentication on a service with no independent identity or user model of its own.
- Removes the need for defensive HTTP-layer hardening that only matters for untrusted callers.
- Avoids the Consumption-plan billing-abuse risk of an anonymous, publicly reachable, pay-per-use endpoint.

## Consequences

**Positive:**

- Simpler engine codebase — no auth middleware or token validation.
- No additional Azure infrastructure or cost beyond hosting the backend itself.
- Smaller attack surface, since there's no public endpoint.
- No exposure to anonymous abuse.

**Negative:**

- Ties the engine's safety to the backend and engine staying in the same Container Apps environment. If either is ever moved elsewhere, this decision has to be revisited.
- If a future requirement needs the engine reachable from somewhere other than the backend (a different first-party service, a future public API), this decision has to be revisited.
- Ties the engine's safety to correctly configured ingress settings.
