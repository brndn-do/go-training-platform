# 16. One KataGo Query at a Time per Engine Instance

Date: 2026-08-14

## Status

Accepted

## Context

This deployment has a fixed, limited CPU budget (ADR 2, ADR 9), and there's more than one way to spend it across incoming queries. KataGo's analysis-engine process can, in principle, handle multiple queries concurrently within a single process via its thread settings — cores could be split across several queries at once instead of given entirely to one. Running multiple separate KataGo processes per instance is another option, each getting its own share of cores.

Splitting cores across concurrent queries means each individual query gets slower, or the cores get oversubscribed instead of adding real throughput — and since no-search play (ADR 15) already makes each query cheap, the throughput gain from that kind of parallelism is marginal to begin with. On the other hand, running multiple separate processes means each process would need its own copy of both loaded neural network models (ADR 15), multiplying the memory and cold-start cost.

## Decision

We will process one query at a time, as fast as possible, rather than split the CPU budget across concurrent queries. We will run exactly one KataGo process per engine instance, with threading configured as follows:

- `numAnalysisThreads = 1` — never analyze more than one query concurrently.
- `numSearchThreadsPerAnalysisThread = 1` — no additional intra-query thread parallelism on top of that.
- `numEigenThreadsPerModel = 4` — the one query that is running gets the full core count, rather than sharing it with others.

We will not parallelize within the process or run multiple processes per instance.

**Reasons:**

- Dedicating the full CPU budget to whichever query is running keeps that query's own latency as low as possible; splitting cores across concurrent queries would slow every one of them down instead.
- No-search play already makes each query cheap, so the throughput gain from queries running in parallel would be marginal.
- A second KataGo process would double the memory/cold-start cost of loading both neural network models (ADR 15).

## Consequences

**Positive:**

- Simple, predictable resource footprint per instance with one model-loaded process, one queue, and no in-process coordination between concurrent queries.
- The full CPU budget goes toward minimizing the latency of whichever single query is running, rather than being split across several.

**Negative:**

- Per-instance throughput is capped at one query's latency at a time; a burst of concurrent requests to one instance queues up rather than running in parallel.
- The only option for more throughput is horizontal scaling (more replicas, ADR 9), not vertical/in-process parallelism — ties the service's scaling story tightly to the deployment platform's replica model.
- If a future deployment target has more CPU headroom, intra-process parallelism becomes an option again.