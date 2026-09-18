# Deployment measurements

Measured 2026-09-17 at `26a6100`, through the API, against backend and engine on Azure Container Apps (Consumption) in Central US, Neon Postgres in `aws-us-east-2`, images from public GHCR.

## Cold start

Engine at 4 vCPU / 8 GiB, `minReplicas: 0`, so each of these is a real scale-from-zero.

| Phase | Time |
| --- | --- |
| Image pull (engine, 1.11 GB compressed; 2.47 GB on disk) | 19.4s |
| Container create to listening | 2.2s |
| KataGo model load | 23s |
| **Worst case** | **64s** |
| Warm | 5s |

One layer accounts for 0.96 GB of the 1.11 GB — the KataGo binary and models.

Image caching is per-node, not per-app, and ACA's node retention isn't documented in a way that can be planned around. 64s is the planning number.

## Per-move latency

9.53–9.64s avergge per human move →  bot move → suggestion reponse cycle. Eigen CPU build, `b40c768` main net, no GPU on ACA Consumption, `numEigenThreadsPerModel = 4` already using every vCPU.

## Decisions

**GHCR stays; ACR is not worth buying.** The 19.4s pull is the smaller half of a cold start, and the KataGo load is paid on every scale-from-zero regardless of registry. Easily reversible.

**10s per request/response is acceptable** for a hobby training project with no time controls.
