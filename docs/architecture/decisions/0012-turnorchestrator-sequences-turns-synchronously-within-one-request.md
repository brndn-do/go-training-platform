# 12. Sequence turns synchronously within one request

Date: 2026-08-14

## Status

Accepted

## Context

After a human move is made, or a user creates/loads a game where it's the bots turn to play, the bot needs to respond with a move (query the engine, record its move). This could be built as a synchronous call chain within one HTTP request, or as an asynchronous flow: a message bus or event system publishing "human moved," a background worker picking it up, and the client polling or subscribing for the bot's response.

## Decision

We will drive the full user → bot → user cycle synchronously, within one orchestrated call. We will not introduce a message bus or event system.

**Reasons:**

- The bot needs to respond before the human's request can be considered complete.
- A message bus/event system with polling or websocket delivery is a heavier lift than a synchronous call chain.

## Consequences

**Positive:**

- Simpler to build, trace, and test than an async/eventing architecture — a single call stack, no separate delivery mechanism, no infrastructure (message broker, websocket server).
- The response to a human's move-making HTTP call includes the bot's reply already, so the frontend doesn't need polling or websocket logic to find out what the bot did.

**Negative:**

- Request latency includes the bot's full response time, so any slowdown in the engine is a slower HTTP response.
- If bot logic later needs to run independently of a user request (e.g. scheduled moves, retries after failure), this design would need to be extended or revisited.
