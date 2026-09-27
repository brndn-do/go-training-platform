# Deployment

What is deployed and how it is configured.

There is no infrastructure-as-code — every resource was created from the CLI. Live values (hostnames, URIs, connection strings) are set on each Azure resource and are not repeated here.

## Topology

Backend and engine share one Container Apps environment in Central US, both on the Consumption plan with `minReplicas: 0`.

| Component | Platform | Notes |
| --- | --- | --- |
| Backend | Azure Container Apps | Public ingress. [ADR 25](architecture/decisions/0025-deploy-backend-on-azure-container-apps.md) |
| Engine | Azure Container Apps | Internal ingress, reachable only from the backend over the environment's internal DNS. 4 vCPU / 8 GiB. [ADR 22](architecture/decisions/0022-deploy-engine-on-azure-container-apps.md), [ADR 23](architecture/decisions/0023-keep-the-engine-internal-not-publicly-reachable.md) |
| Postgres | Neon, `aws-us-east-2` | [ADR 31](architecture/decisions/0031-host-postgres-on-neon.md) |
| SPA | Azure Static Web Apps, Free — decided, not yet deployed | [ADR 30](architecture/decisions/0030-host-the-spa-on-azure-static-web-apps.md) |

Images live in public GHCR as `ghcr.io/brndn-do/go-training-platform-{backend,engine}`, tagged with the commit SHA, and are built and pushed by hand. Pushing needs a **classic** PAT with `write:packages` — GHCR rejects fine-grained tokens.

Cold-start and per-move timings are in [deployment-measurements.md](deployment-measurements.md).

## Data protection key ring

[ADR 32](architecture/decisions/0032-persist-data-protection-keys-in-blob-storage.md). The key ring is a blob, wrapped by a Key Vault key. Four settings configure it; `.env.example` carries their shape:

```
DataProtection__Provider=AzureBlob
DataProtection__ApplicationName=GoTrainingPlatform
DataProtection__BlobUri=https://<account>.blob.core.windows.net/<container>/keyring.xml
DataProtection__KeyVaultKeyUri=https://<vault>.vault.azure.net/keys/<key>
```

Two resources back it:

- A **storage account** holding `keyring.xml`. Shared-key auth is disabled, so every access goes through an Entra identity.
- A **key vault** holding the key that wraps the key ring. RBAC permission model, soft delete on, purge protection **off**.

The backend's system-assigned identity holds `Storage Blob Data Contributor` on the container and `Key Vault Crypto User` on the key — scoped to those, not to the account and the vault.

### Two things that break every session

- **`ApplicationName` can never change.** It is part of the key ring's identity, so changing it signs everyone out.
- **Never delete an old Key Vault key version.** Each data protection key records the version that wrapped it, so removing one orphans everything still encrypted under it. Purge protection is off, so nothing prevents this.

### Verified 2026-09-27

A cookie issued by one container was accepted by its replacement after the first was destroyed, and `keyring.xml` was not rewritten across the restart — the new container loaded the existing key ring instead of generating one. The blob holds `<encryptedSecret>`/`<encryptedKey>` under `AzureKeyVaultXmlDecryptor`, with no plaintext `<masterKey>`.