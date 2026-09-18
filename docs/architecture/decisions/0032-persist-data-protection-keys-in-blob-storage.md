# 32. Persist data protection keys in Blob Storage, encrypted with Key Vault

Date: 2026-09-18

## Status

Accepted

## Context

We are using ASP.NET Core Identity cookies (ADR 29). Data Protection generates the keys that encrypt and sign that cookie, and by default keeps them with the running instance.

The backend runs on Container Apps (ADR 25) with `minReplicas: 0`, so the container is destroyed whenever the app is idle. Each new container generates a fresh key and cannot decrypt any cookie issued before it. More than one replica has the same problem in parallel: each holds its own keys, so a cookie issued by one is rejected by another.

The keys are sensitive, as whoever can read them can forge a cookie for any user.

We need to decide where the key ring lives and how it is protected at rest.

## Options Considered

**1. Leave the keys with the running instance**

- **Pros:** Nothing to build or provision.
- **Cons:** Sessions do not survive a restart or a scale-from-zero, and cannot be shared across replicas. Incompatible with `minReplicas: 0`.

**2. Persist to PostgreSQL**

- **Pros:** No new resource and no new credential; uses the database the backend already has.
- **Cons:** The keys share a trust boundary with the data they protect — read access to the database escalates from reading user data to impersonating any user. Protecting them at rest still needs a mechanism from outside that boundary.

**3. Persist to Azure Blob Storage, encrypt at rest with Azure Key Vault, authenticate with a managed identity**

- **Pros:** The key-encryption key sits behind a credential the database cannot reach, so a database compromise does not yield forgeable sessions. Managed identity removes the stored credential entirely. The conventional arrangement for ASP.NET Core on Azure.
- **Cons:** Two more Azure resources on a deployment that has no infrastructure as code. Losing the vault key destroys every session permanently.

**4. Supply a fixed key through configuration**

- **Pros:** Familiar from symmetric-token schemes.
- **Cons:** Unsupported by Data Protection, so it means replacing the key repository. Loses automatic rotation and revocation, and puts raw key material in the platform's configuration.

## Decision

We will persist the key ring to Azure Blob Storage, encrypt it with a key held in Azure Key Vault, and authenticate to both with a managed identity.

**Reasons:**

- Keeping the key-encryption key outside the database is the difference between a database compromise leaking data and it granting the ability to impersonate any user.
- Managed identity means no credential exists to leak or rotate.
- Sessions surviving restarts is a precondition for `minReplicas: 0`.

## Consequences

**Positive:**

- Sessions will survive restarts, scale-from-zero, and deploys, and will be valid across replicas.
- No credential for the key store will exist in configuration.
- A database compromise alone will not yield forgeable session cookies.

**Negative:**

- Two more Azure resources will exist only as CLI history, like the rest of the deployment.
- Losing the Key Vault key will invalidate every session, permanently and unrecoverably.
- The vault will be reachable over the public internet without VNet (ADR 23). Access will rest on RBAC rather than network isolation.
- Local development and the test suite will need a key store that is not the Azure one.
- Changing the key store later will sign every user out once.
