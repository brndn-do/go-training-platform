# The auth API

| Method | Route                | Notes                                                        |
| ------ | -------------------- | ------------------------------------------------------------ |
| `POST` | `/api/auth/register` | `{ email, password }`. 204. Does not sign the user in.       |
| `POST` | `/api/auth/login`    | `{ email, password, rememberMe }`. 204 and a session cookie. |
| `POST` | `/api/auth/logout`   | 204, whether or not a session existed.                       |
| `GET`  | `/api/auth/me`       | 200 `{ user }`. `user` is `null` when nobody is signed in.   |

## Things to build against

- The session is a cookie the browser stores and sends by itself. JavaScript can't read it, and it's only sent over HTTPS.
- Requests from another origin must include cookies: `fetch(url, { credentials: 'include' })`. The API's CORS policy allows credentialed requests from the origins in `Cors__AllowedOrigins` (`http://localhost:5173` locally).
- The email is also the username, and emails are unique.
- Passwords need at least 8 characters, with no rules about which kinds of characters. Email and password are each capped at 256 characters.
- Register's `400` lists what went wrong as keys under `errors`: a field name for a malformed request, or an Identity error code such as `DuplicateEmail` or `PasswordTooShort`.
- Login answers every failure with the same `401`, whether the email is unknown, the password is wrong, or the account is locked out. The response doesn't say which.
- `rememberMe: true` keeps the session after the browser closes. Otherwise the cookie is cleared when the browser closes.
- Every game endpoint requires a session. Without one, it answers `401` with a problem details body.
- `/api/auth/me` is how the SPA finds out on page load whether a session already exists — the cookie is `HttpOnly`, so JavaScript can't check for itself. It answers `200` either way, so a signed-out visitor is a success with `user: null`, not a `401`. Reserve the 401 path for a session that expires mid-use.
- `me`'s `user` is `{ id, email }`. The email is a snapshot from when the cookie was issued, not a live read.
