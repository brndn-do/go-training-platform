# 30. Host the SPA on Azure Static Web Apps

Date: 2026-08-29

## Status

Accepted

## Context

ADR 7 chose a React SPA, which compiles to static assets. ADR 25 puts the backend on Azure Container Apps; ADR 23 keeps the engine internal to that environment.

ADR 29 carries the session in a cookie, so the SPA and the API must share a registrable domain for the browser to treat it as first-party. Azure's default hostnames — `*.azurestaticapps.net` and `*.azurecontainerapps.io` — are different sites, so they do not satisfy this on their own.

DNS is on Cloudflare. Azure issues free managed certificates for custom domains, validated by DigiCert over the public internet. That validation needs the hostname to resolve directly to Azure; an intermediate proxy blocks it.

We need to decide what hosts the SPA, and how it and the API come to share a domain.

## Options considered

1. **Azure Static Web Apps, Free plan** — SPA and API on separate subdomains of one domain (`app.example.com`, `api.example.com`).
    - Pros: Free, with a CDN and self-renewing certificates. The SPA deploys without touching the backend image. Same site, so the cookie is first-party.
    - Cons: Different origins, so the API needs CORS with credentials. Two certificates to keep valid. Free allows 2 custom domains and 250 MB, and refuses traffic past 100 GB per month rather than billing for it.

2. **Static Web Apps, Standard plan, with the Container App as a linked backend** — one hostname, `/api` proxied to the backend.
    - Pros: One origin, so CORS does not arise. One hostname, one certificate.
    - Cons: Linked backends are Standard-only, so this costs money. Caps requests at 45 seconds, HTTP only.

3. **Serve the SPA from the backend Container App**, as static files with an SPA fallback route.
    - Pros: One origin, one certificate, one deployment, no extra resource.
    - Cons: Ties the SPA's releases to the backend's, against ADR 3. No CDN, and the first page load wakes a scale-to-zero container.

4. **A second Container App** serving the assets from a web-server image, on its own subdomain.
    - Pros: One hosting product and one pipeline shape throughout, per ADR 25.
    - Cons: Pays compute to serve static files, with no CDN, and adds an image to build.

5. **Cloudflare Workers with static assets** — serves the SPA and proxies `/api` to the Container App on one hostname. **Note:** Cloudflare Pages is the older sibling and still supported, but Cloudflare directs new projects to Workers.
    - Pros: One origin at no cost, so CORS does not arise and `SameSite=Strict` becomes available. The backend can stay on its default hostname, so neither side needs an Azure managed certificate. Static-asset requests are unmetered, and the file and domain limits exceed option 1's.
    - Cons: Puts one component on a second vendor, with its own deployment, credentials, and monitoring. The Container App stays publicly reachable, so the proxy is bypassable unless its ingress is restricted separately. Requests to `/api` count against 100,000 daily Worker invocations and are refused with `429` beyond that. Adds a hop and a toolchain.

## Decision

We will host the SPA on Azure Static Web Apps, Free plan, on a subdomain of the same registrable domain as the API, with both hostnames resolving directly to Azure and using Azure-issued certificates.

**Reasons:**

- The API, engine, and database are already on Azure. Keeping the SPA there holds the system to one resource model, one access-control model, one CLI, and one bill.
- The SPA keeps its own deployment, preserving ADR 3's independent versioning.
- Static assets come from a CDN, so the first page load does not pay a container cold start.

Option 5 is stronger on the merits: it removes CORS and both managed certificates at no cost. It is rejected for cloud consolidation, not on technical grounds, and should be revisited if the CORS or certificate work proves troublesome.

## Consequences

**Positive:**

- Assets are CDN-served with self-renewing certificates, at no cost.
- The SPA and backend deploy independently.
- The cookie is first-party in production.
- One cloud, one credential model, one monitoring surface.

**Negative:**

- Certificate expiry becomes a condition to monitor. The SPA's hostname and the API's each carry their own Azure-managed certificate, issued by a different service and renewing independently, and either proxying the hostname or a missing CAA entry blocks renewal silently, months after the change that caused it. A failed renewal takes the site down.
- Preview environments cannot authenticate. Static Web Apps serves them from `*.azurestaticapps.net`, a different site from the API, so the session cookie is never sent and their origins are too dynamic to allowlist. Pull-request previews are limited to signed-out behavior.
- Traffic past 100 GB per month is refused rather than billed, so exceeding it is an outage with no warning. The SPA's two custom-domain slots must cover every hostname it answers on, so serving the apex and `www` later means retiring `app.example.com` rather than adding to it.
