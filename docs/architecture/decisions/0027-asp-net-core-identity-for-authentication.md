# 27. ASP.NET Core Identity for authentication

Date: 2026-08-25

## Status

Accepted

## Context

User accounts do not exist yet. `ICurrentPlayer` is registered only under `IsDevelopment()`, and with `ValidateOnBuild` the host fails at `Build()` in any other environment. The backend cannot start outside Development until this is resolved, which blocks deployment (ADR 24, ADR 25).

What is at stake is credential storage, password hashing, lockout, and reset — the parts of authentication that fail quietly when they are wrong.

`.env.example` already carries `Jwt__Secret`/`Issuer`/`Audience` placeholders, unused by any code.

We need to decide what owns credentials and the sign-in surface.

## Options considered

1. **ASP.NET Core Identity.** The framework's own user store, password hashing, and sign-in manager, backed by our Postgres schema.
    - Pros: Hashing, lockout, and reset arrive already written and widely reviewed. No external dependency, so local development and tests stay self-contained.
    - Cons: Its EF model arrives whole — seven tables, or four without roles — where this app's own model needs one. Credentials become ours to store and operate.

2. **External identity provider** (Entra ID, Auth0, or similar).
    - Pros: No credential storage, password reset, or lockout logic to own or secure.
    - Cons: A hard external dependency for local development and tests, and a tenant to configure and keep working. The subject claim is provider-shaped, not a `Guid`.

3. **Hand-rolled authentication** — own user table, own password hashing, own token issuing.
    - Pros: Total control over schema, endpoints, and key types; nothing to bend to.
    - Cons: Credential storage, hashing, and token validation are the parts of this most likely to be got wrong.

## Decision

We will use ASP.NET Core Identity to own credentials and the sign-in surface.

**Reasons:**

- Credential storage, hashing, lockout, and reset are the parts most likely to get wrong, and to look fine when they are. Identity owns them; we do not.
- Local development and tests stay self-contained, with no tenant to configure or pay for.
- Identity issues either a cookie or a bearer token, so the transport stays a separate decision.

## Consequences

**Positive:**

- Hashing, lockout, and reset ship with the framework.
- No external tenant in the path of local development or tests.

**Negative:**

- Identity's schema arrives whole — seven tables, or four without roles via `IdentityUserContext`. This app uses one.
- The key type must be settled in the initial migration; changing it later means dropping and re-creating Identity's tables.
- Credential storage becomes ours to operate: reset, lockout, and any breach response.
