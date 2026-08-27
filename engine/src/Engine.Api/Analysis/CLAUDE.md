# CLAUDE.md — Analysis

Query construction and response interpretation. Dependency-free: does not touch a process, a socket, or a file. If something needs IO, it belongs in `Processes/`.

`IKataGoClient` is defined here and implemented in `Processes/`.

## Strength tiers

The same response is read two different ways:

- **`Superhuman`**, and _every_ player hint (regardless of the game's bot strength), takes **argmax** over `policy` (the self-play network).
- **Ranked ranks** (`Kyu1`–`Kyu20`, `Dan1`–`Dan9`) **sample proportionally** from `humanPolicy` (the human-SL network), clamping KataGo's `-1` illegal-point entries to zero first.

`BotStrength` validates the rank string at construction, so an invalid one fails at the edge rather than reaching KataGo.

## Serialization

- KataGo JSON is camelCase, on both serialize and deserialize.
- Keep `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on `KataGoQuery.OverrideSettings`. Removing it breaks Superhuman queries.
- Win rates are always Black's — don't flip them here.

## Tests

Pure unit tests plus `TestData/*.json` captured from real KataGo responses. **Prefer adding a captured response over hand-writing JSON** — a hand-typed fixture only proves the deserializer accepts what you typed.

`SuggestionServiceIntegrationTests` drives the real binary and needs the `KataGoProcess__*` env vars.
