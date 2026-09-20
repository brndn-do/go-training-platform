# Next: #36 — provision the Azure key store

The code is done and committed: `DataProtection__Provider` selects the key store (`AzureBlob` | `FileSystem` | `Ephemeral`), `SetApplicationName` is pinned for every mode, and the key ring is forced open before `app.Run()` so an unreachable store fails the boot. Three tests on the `PostgresApi` collection cover a cookie surviving a replacement host, and being rejected across a different key directory or a different application name.

`FileSystem` is what runs locally and under compose (a named volume). **The `AzureBlob` branch has never executed** — nothing in the suite reaches it, and nothing can until the resources exist.

## To do

1. **Azure resources by CLI** — storage account + blob container (create the container yourself), Key Vault on the **Azure RBAC** permission model, versionless key id. The managed identity needs **Storage Blob Data Contributor** and **Key Vault Crypto User** — data-plane roles, not covered by `Contributor`.
2. **Set `DataProtection__Provider=AzureBlob`, `DataProtection__ApplicationName` and the two URIs on the Container App.** Nothing in the repo carries them; deployment is CLI history (ADR 32). `BlobUri` names the blob, not its container.
3. **Verify deployed** — sign in, force a new revision, replay the cookie. A boot failure here means RBAC, and the container will say so rather than starting and failing at first login. Check whether Defender for Cloud auto-enables on the new storage account or vault.
4. **Close #36** with `Fixes #36`, and drop its `needs-decision` label — ADR 32 settled it.

Security review is warranted before this ships; it is session authentication, and a wrong `ApplicationName` is invisible until sessions break in production.

Out of scope: the post-deploy smoke test (its own issue).

Docs: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview
