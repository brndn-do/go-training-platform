# Next: #36 — session cookies do not survive a container restart

Approach is settled in ADR 32 and the issue's implementation notes. Config selection is **Option A: an explicit `DataProtection__Provider` mode** (`AzureBlob` | `FileSystem` | `Ephemeral`), chosen over inferring the mode from which settings are present, because a typo'd URI would otherwise fall through to ephemeral and boot green.

## Scaffolding already in the tree (uncommitted)

- `Api/DataProtectionOptions.cs`, `Api/DataProtectionProvider.cs` — options type and mode enum. Enum starts at 1 so `default` is not a valid mode.
- `.env.example` — new `DataProtection__*` block, `FileSystem` locally.
- `.gitignore` — `.dataprotection-keys/`.
- `PostgresApiFixture.CreateSeparateHost(keyRingPath, applicationName)` — builds an independent host over the same Postgres container. Shared settings extracted into `BuildFactory`; the fixture's own host is `Ephemeral` with `DefaultApplicationName`.

Backend builds clean (only the pre-existing `SA0001` / `NU1903` warnings).

## To do

1. **Three tests first**, in `Api.Tests` on the `PostgresApi` collection:
   - session issued by host A is still valid on a fresh host B sharing the same temp key directory;
   - not valid when B has a *different* directory;
   - not valid when B pins a different `ApplicationName` (catches the `SetApplicationName` failure that persistence alone does not).
   Clients hold their own `CookieContainer`, so replay the cookie across hosts by hand — read `Set-Cookie` via `CreateCookielessClient`, then set it as a request header on host B.
2. **`Program.cs`** — bind and validate `DataProtectionOptions` with `AddOptionsWithValidateOnStart`, `SetApplicationName` unconditionally, branch on the mode. An unrecognized `Provider` string throws at bind time rather than producing a validation message; decide whether to catch and reword.
3. **Force the key ring open after `builder.Build()`** — it is lazy, so a missing RBAC role otherwise boots healthy and fails on first login.
4. **Azure resources by CLI** — storage account + blob container (create the container yourself), Key Vault on the **Azure RBAC** permission model, versionless key id. Managed identity needs **Storage Blob Data Contributor** and **Key Vault Crypto User** — data-plane roles, not covered by `Contributor`.
5. **Verify deployed** — sign in, force a new revision, replay the cookie. Check whether Defender for Cloud auto-enables on the new storage account or vault.

Packages: `Azure.Extensions.AspNetCore.DataProtection.Blobs`, `Azure.Extensions.AspNetCore.DataProtection.Keys`, `Azure.Identity`. The `Microsoft.AspNetCore.DataProtection.Azure*` packages are superseded.

Out of scope: the post-deploy smoke test (its own issue).

Docs: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
