# 4. ASP.NET Core for the backend API

Date: 2026-08-14

## Status

Accepted

## Context

The backend needs a web framework that serves an HTTP API to the frontend.

## Decision

We will build the backend API on ASP.NET Core, using controllers.

**Reasons:**

- We want a mature dependency-injection container the architecture can be wired through.
- We want first-class integration with the chosen ORM and database driver.
- Controllers match a growing REST resource model.

## Consequences

**Positive:**

- Access to a mature, built-in dependency-injection container.
- Well-supported integration with its ORM and the database driver.

**Negative:**

- Controllers bring more structure and ceremony than minimal APIs for a small API surface today, accepted deliberately in anticipation of a growing REST resource model.
- Ties the backend to the .NET runtime and its release/support cadence for the life of the project.
