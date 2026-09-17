# 31. Host Postgres on Neon

Date: 2026-09-17

## Status

Accepted

## Context

ADR 5 chose PostgreSQL as the database. We haven't decided what runs it once the application is deployed; local development uses the `postgres` container in `docker-compose.yml`.

The backend and engine are deployed on Azure Container Apps (ADR 25, ADR 22) on the Consumption plan, chosen so that neither service costs anything while idle. The database is the remaining component whose hosting is unsettled.

ADR 30 hosts the SPA on Azure Static Web Apps, and rejected an option it judged stronger on the merits in order to keep the system on a single cloud provider.

Expected usage is a handful of users at a time, with long idle periods. The data is game records and Identity user rows; there is no regulated or high-value data, and no availability commitment to anyone.

We need to decide what hosts PostgreSQL in the deployed environment.

## Options Considered

**1. Azure Database for PostgreSQL Flexible Server, Burstable B1ms**

- **Pros:** Managed patching, automated backups, and point-in-time restore. Stays inside the Azure resource, access-control, and billing model already used by every other component. Can be reached over a private endpoint rather than the public internet. The conventional choice in an Azure-hosted .NET system.
- **Cons:** Bills continuously whether or not anyone is using the application — the only component that would. Private networking requires a VNet, which is the infrastructure ADR 23 was arranged to avoid; the alternative is a public endpoint with firewall rules.
- **Cost:** Roughly $12–15 per month plus storage. New Azure accounts include a 12-month free grant that covers a B1ms instance, after which the full cost applies.

**2. PostgreSQL as a container in the Container Apps environment**

- **Pros:** No additional vendor or hosting product. Mirrors the local `docker-compose.yml` topology.
- **Cons:** Container Apps storage is ephemeral, so persistence requires mounting Azure Files — which costs money and fails quietly if misconfigured. No backups, no point-in-time restore, and no managed patching. Scaling the app to zero is incompatible with the database living inside it.
- **Cost:** Container Apps consumption for a continuously-running replica, plus Azure Files.

**3. Neon**

- **Pros:** The free tier costs nothing and suspends compute when idle. Managed backups and point-in-time restore are included. Provisioning is a connection string; there is no infrastructure to define.
- **Cons:** A second vendor, with its own account, credentials, and status page. New projects are AWS-only, so database traffic leaves Azure and crosses the public internet. A project's region is fixed at creation. Suspended compute adds its own cold start on top of the Container Apps cold start. Free-tier storage and compute quotas apply, the free tier carries no availability commitment, and inactive free-tier projects are deleted.
- **Cost:** Free at the expected scale.

## Decision

We will host PostgreSQL on Neon, using the free tier, with the connection string supplied to the backend as a Container Apps secret.

**Reasons:**

- Every other hosting decision was made to reach zero cost while idle.
- Moving to Azure later is a dump, a restore, and a changed connection string. The cost of reversing this is low enough.
- Backups and point-in-time restore are present on the free tier.

## Consequences

**Positive:**

- The deployed system will cost nothing while idle.
- Backups and point-in-time restore will be available without configuring or paying for them.
- No VNet, private endpoint, or firewall rules will be needed to reach the database.
- Provisioning will not add infrastructure to define or maintain.

**Negative:**

- The system will span two vendors. An outage or a change of terms on either side can take the application down, and there will be two status pages to watch. Neon deprecated all three of its Azure regions in April 2026, giving roughly six months to migrate off them.
- Database traffic will leave Azure and cross the public internet. Confidentiality will rest on TLS and on the connection secret rather than on network isolation. Since new projects are AWS-only, this is permanent rather than something a later region change could fix.
- Every query will pay a cross-provider round trip, on a request path that already carries two cold starts. The closest region to an Azure Central US deployment is AWS US East (Ohio).
- The region is fixed at creation, so correcting the pairing later means a new project and a data migration.
- The database credential will be a stored secret requiring manual rotation, where an Azure-hosted database could have used managed identity.
- A cold request will pay Neon's resume time in addition to the Container Apps cold start.
- Free-tier quotas will become a condition to monitor, and exceeding them degrades or suspends the database. A project left inactive for months will be deleted outright.
- Outgrowing the free tier means choosing between paying Neon and carrying out the migration to Azure that this decision defers.
