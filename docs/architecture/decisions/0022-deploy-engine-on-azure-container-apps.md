# 22. Deploy engine on Azure Container Apps

Date: 2026-08-15

## Status

Accepted

## Context

The engine is being packaged as a Docker container (ADR 18). Something needs to actually run that container.

We want zero cost while idle, a single healthy instance when there's demand (a handful of users at a time, not production-scale concurrency), and automatic detection and recovery from problems without a human having to manually restart or stop an instance.

Real options considered:

- **Azure Container Apps** — fully-managed serverless containers, scale-to-zero on the Consumption plan, native support for Kubernetes-style startup/readiness/liveness probes.
- **Azure Kubernetes Service (AKS)** — real Kubernetes, maximum flexibility, but no meaningful scale-to-zero (the node pool is billed continuously even with zero pods scheduled), plus operational overhead (cluster/node management).
- **Azure Web App for Containers** — App Service running a container instead of code-deploy, but weaker scale-to-zero than Container Apps.
- **Azure Container Instances (ACI)** — the simplest way to run a single container, but no built-in orchestration or health-probe model at all; we'd have to hand-build the detection/recovery behavior ourselves.
- **A non-Azure option** (e.g. Google Cloud Run) — comparable to Container Apps in capability, but the backend is already on Azure App Service (ADR 20), so this would mean two cloud providers, two sets of credentials/IAM, and cross-cloud networking instead of staying inside one vendor.

## Decision

We will deploy the engine on Azure Container Apps, using the Consumption plan with `minReplicas: 0` and a small `maxReplicas` cap — no horizontal-scale-under-load rules are needed given the expected traffic.

**Reasons:**

- Scale-to-zero: no cost while idle, which is the primary requirement given expected usage.
- Native support for the startup/readiness/liveness probe model decided on (ADR 21) — nothing to hand-build or approximate.
- Keeps the whole deployment on one cloud provider alongside the backend (ADR 20).
- Avoids operational overhead compared to something like AKS.

## Consequences

**Positive:**

- No cost while idle; billed only for actual usage when a request is being served.
- The probe design (ADR 21) maps directly onto the platform.
- Automatic restart-on-liveness-failure and removal-from-routing-on-readiness-failure both happen without custom orchestration code, once the probes report state correctly.
- Simple scale configuration given expected load.

**Negative:**

- Ties the engine's deployment mechanism specifically to Azure Container Apps.
- Scale to zero means accepting cold-start latency.
