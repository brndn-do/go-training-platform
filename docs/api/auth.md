# The auth API

| Method | Route                | Notes                                                        |
| ------ | -------------------- | ------------------------------------------------------------ |
| `POST` | `/api/auth/register` | `{ email, password }`. 204. Does not sign the user in.       |
| `POST` | `/api/auth/login`    | `{ email, password, rememberMe }`. 204 and a session cookie. |
| `POST` | `/api/auth/logout`   | 204, whether or not a session existed.                       |

## Things to build against

- The session is a cookie the browser stores and sends by itself. JavaScript can't read it, and it's only sent over HTTPS.
- Requests from another origin must include cookies: `fetch(url, { credentials: 'include' })`. The API has no CORS policy yet (#33), so the SPA can't call it from its own origin until that lands.
- The email is also the username, and emails are unique.
- Passwords need at least 8 characters, with no rules about which kinds of characters. Email and password are each capped at 256 characters.
- Register's `400` lists what went wrong as keys under `errors`: a field name for a malformed request, or an Identity error code such as `DuplicateEmail` or `PasswordTooShort`.
- Login answers every failure with the same `401`, whether the email is unknown, the password is wrong, or the account is locked out. The response doesn't say which.
- `rememberMe: true` keeps the session after the browser closes. Otherwise the cookie is cleared when the browser closes.
- Every game endpoint requires a session. Without one, it answers `401` with a problem details body.
