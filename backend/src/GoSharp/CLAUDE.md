# CLAUDE.md — GoSharp (vendored)

Third-party Go rules engine, copied as source from [paviad/GoSharp](https://github.com/paviad/GoSharp), MIT. `VENDORED.md` has the provenance.

## Rules

- **Don't edit or reformat it.** It is exempt from StyleCop, from doc-comment enforcement, and from the repo's `sealed`-by-default convention.
- The namespace stays `GoSharpCore`, unchanged from upstream.
- **Only `Domain` may reference it**, and only through `GoPosition`.

## Behaviors to note

- `Game.MakeMove(x, y)` returns a **new** `Game` rather than mutating in place.
- It reports legality via `out bool legal` but does **not** enforce it.
- It **throws** on out-of-range coordinates instead of reporting them as illegal.
- `Game.SetupMove` does **not** resolve captures — it is a raw board write meant for problem setup. Rebuilding a saved game must replay with `MakeMove(x, y)` in order, or captured stones wrongly remain on the board. Nothing in this codebase calls `SetupMove`.
