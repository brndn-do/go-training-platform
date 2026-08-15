# 7. React and Vite for the frontend

Date: 2026-08-14

## Status

Accepted

## Context

The frontend needs to render an interactive Go board, handle real-time-feeling interaction, and talk to the backend's HTTP API, which is a stateful, component-heavy UI.

## Decision

We will build the frontend as a React single-page application, using Vite as the build tool and dev server.

**Reasons:**

- The UI is stateful and component-heavy (board, move history, hint panel, bot-strength controls), not a mostly-static site.
- Fast local iteration matters for a UI that will be tuned a lot by feel.

## Consequences

**Positive:**

- A component model that fits the actual UI shape well — a board, move history, hint panel, and bot-strength controls are independent, composable pieces of state and rendering.
- Fast local iteration: Vite's dev server is built around near-instant reloads.
- The frontend becomes its own independently-versioned stack (see ADR 3), with its own dependency graph and release cadence, decoupled from the backend and engine's own .NET tooling and upgrade cycles.

**Negative:**

- Commits the project to the JavaScript/TypeScript ecosystem's own pace of change for this one stack, separate from and faster-moving than the .NET ecosystem.
