# 29. Use Identity authentication cookie

Date: 2026-08-26

## Status

Accepted

## Context

ADR 27 chose ASP.NET Core Identity. Identity can hand the browser either an authentication cookie or a bearer token, and nothing has decided which. `.env.example` carries `Jwt__Secret`/`Issuer`/`Audience` placeholders that no code reads.

The SPA and the API are served separately. In development the SPA is on `:5173` and the API on `:5000` — different origins, but the same site. Production hostnames are not chosen yet. If the two end up on different sites, a session cookie becomes a third-party cookie, which Safari blocks by default and Chrome restricts.

ADR 23 gives the engine no authentication of its own, so nothing else in the system needs to read a credential issued here.

Each kind of credential has one characteristic weakness. A credential the browser attaches on its own can be triggered by another site the user happens to visit — cross-site request forgery. A credential JavaScript can read can be stolen outright if a script is ever injected into the page — cross-site scripting.

We need to decide where the session credential lives in the browser, and how it reaches the API.

## Options considered

These are not mutually exclusive — options 3 and 4 each combine parts of the first two.

1. **An Identity authentication cookie.** The browser stores it and attaches it to every request on its own.
    - Pros: JavaScript cannot read it, so an injected script cannot steal it. Identity ships the handler and can revoke outstanding cookies. The SPA needs no authentication code beyond asking the browser to send credentials.
    - Cons: The browser also attaches it to requests started by other sites, so CSRF is ours to mitigate. The SPA and API must share a registrable domain, which constrains where each is hosted.

2. **A bearer token held in JavaScript**, in `localStorage` or in memory, added to an `Authorization` header on each request.
    - Pros: Nothing is attached automatically, so CSRF does not arise. The SPA and API can sit on different sites, so hosting is unconstrained.
    - Cons: An injected script can read the token and reuse it from anywhere until it expires. The SPA owns storage, header attachment, and refreshing. Revoking a token early needs a server-side blocklist.

3. **A short-lived token in memory, refreshed through a long-lived cookie.**
    - Pros: A leaked token is useful only briefly, and the long-lived credential stays out of JavaScript's reach.
    - Cons: The refresh credential is still a cookie, so hosting is constrained exactly as in option 1. An injected script can call the refresh endpoint and mint new tokens anyway. Without token rotation this is option 1 with more moving parts.

4. **A JWT carried in an `HttpOnly` cookie.**
    - Pros: A standard format that other services could read on their own.
    - Cons: Takes on the cookie's CSRF exposure and the token's revocation problem together. ASP.NET Core's JWT handler reads the `Authorization` header, so reading one from a cookie is custom plumbing.

## Decision

We will carry the session in an ASP.NET Core Identity authentication cookie, and host the SPA and the API under one registrable domain so the browser treats that cookie as first-party.

The cookie is `HttpOnly` (JavaScript cannot read it), `Secure` (sent over HTTPS only), `SameSite=Lax` (not sent on requests started by other sites), and host-only (scoped to the API's exact hostname, not shared with sibling subdomains).

We will defer antiforgery tokens for now, relying on `SameSite=Lax` as the CSRF mitigation until a second project or environment shares the domain.

**Reasons:**

- CSRF, the cookie's weakness, has mitigations (SameSite, CSRF tokens) that neutralize the vulnerability itself. A bearer token's weakness, theft via XSS, has no equivalent mitigation for the theft step: once JS executes, any token it can read, it can exfiltrate. Defenses only reduce the odds of XSS occurring (CSP, sanitization) or limit the blast radius afterward (short-lived tokens, in-memory storage, etc).
- `SameSite=Lax` already blocks the requests antiforgery exists to stop.
- Antiforgery is deferred because a sibling subdomain counting as the same site, does not exist while our domain serves only this project. The exposure is also low: no payments, and the actions an attacker could forge are moves in a practice game.
- Adding antiforgery later is additive.
- Identity's cookie already carries its claims, encrypted and signed. A JWT's advantage is that other services can read it, and by ADR 23 nothing else needs to.
- Identity's security stamp revokes outstanding cookies on a credential change. Matching that with a self-contained token would mean a blocklist.
- The SPA needs no authentication code beyond asking the browser to send credentials.

## Consequences

**Positive:**

- The SPA never holds a credential it can read, so an injected script cannot steal a session for use elsewhere.
- No token storage, refresh flow, or concurrent-refresh races in the SPA.
- Revoking a session is built in, with no blocklist to maintain.

**Negative:**

- Any subdomain added to this domain later counts as the same site, and could forge authenticated requests. Revisit antiforgery before hosting a second project or environment there.
- The SPA and API must share a registrable domain, and requests between them need a CORS policy naming the SPA's origin with credentials allowed. Azure's default hostnames are different sites, so neither works as-is.
- Data Protection keys must be shared and persisted across replicas, or a cookie issued by one is rejected by another and users are signed out at random.
- A non-browser client, if added, would need a second way to authenticate.
