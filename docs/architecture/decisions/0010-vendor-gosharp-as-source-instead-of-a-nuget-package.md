# 10. Vendor GoSharp as source

Date: 2026-08-14

## Status

Accepted

## Context

The backend needs a real Go rules engine for verifying board state, captures, ko, and move legality fast without relying on KataGo or blindly trusting clients. GoSharp, a suitable MIT-licensed implementation in C#, fits, but no package feed distributes it.

## Decision

We will vendor GoSharp as source directly into the codebase, rather than reimplementing Go rules from scratch or publishing/consuming a private package feed. The Domain layer will wrap it behind an adapter, so no outer layer ever depends on the vendored library's own types directly (see ADR 8).

**Reasons:**

- Need a real, tested Go rules engine without reimplementing our own.
- GoSharp is implemented using C#, matching that of our backend.
- GoSharp fits, but no package feed distributes it, so vendoring source is the only distribution option.

## Consequences

**Positive:**

- Tested Go rules logic (captures, ko, legality, and board state) without reimplementing a tricky domain from scratch.

**Negative:**

- No upstream release to pull fixes from — any fix or update to the vendored library's own logic has to be manually pulled in and re-vendored.
- The vendored library's own API surface is a permanent constraint we have to design around.
