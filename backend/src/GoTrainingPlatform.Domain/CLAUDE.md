# CLAUDE.md — Domain

The innermost layer: entities, value objects, and every Go rule. References nothing but the vendored `GoSharp`.

- `Game.cs` — the aggregate root. The only place rules live.
- `GoPosition.cs` — the adapter over `GoSharp`. Board state and legality for one position.
- `Move.cs`, `Coordinates.cs`.
- `Enums/` — `Color`, `Content`, `Outcome`, `BotStrength`.

## Two-step construction

**Every `Game` constructor leaves the internal position empty.** `BuildPosition()` must be called explicitly before `Turn`, `GetBoard`, or any `Try*` method is used.

- Calling one of those first throws `InvalidOperationException` from `RequirePosition()` — deliberate, so a half-built aggregate fails loudly instead of reporting an empty board.
- `TryRecordResign` is the one exception: resigning is not turn-gated, so it never reads the position.
- `BuildPosition` rebuilds from scratch by replaying `Moves` in order. `TryUndo` trims history and calls it again rather than stepping the position backward.

Replay must go through `TryMakeMove`, never `GoSharp`'s `SetupMove`, or captured stones wrongly survive — see [../GoSharp/CLAUDE.md](../GoSharp/CLAUDE.md).

## Behaviors

`TryRecordMove`, `TryRecordPass`, `TryRecordResign`, `TryUndo`, and `GoPosition.TryMakeMove` all return `bool` and **leave state unchanged on `false`**. A rejected action is not an exception.

Two consecutive passes end the game without a winner — no scoring. `Komi` is carried through to `GoSharp` for consistency, but nothing in the domain scores with it.

Guard order in `Game` is finished → whose turn → legality. `GoPosition` bounds-checks before delegating, since `GoSharp` throws on out-of-range coordinates.

`Turn` is derived from the position, `BotColor` is derived from `PlayerColor`. Only `Outcome` is mutable, and `null` means in progress.

## Shaped by EF Core

`Move`'s scalar-only constructor exists so EF Core can bind through it. EF can't constructor-bind a navigation property, and `Coordinates` is an owned reference.

## Tests

`Domain.Tests` is pure unit — no fakes, no fixtures, no collection, nothing that leaves the process.

`GameTests` covers the aggregate, including the position-not-built throw for each affected method. `GoPositionTests` covers the bounds check, board snapshots, and what `TryMakeMove` does with each answer `GoSharp` gives back.

**Suicide and ko are deliberately untested.** Testing them would assert that `GoSharp` rules correctly, which is its job, not ours. The occupied-point tests our illegal move path; suicide and ko are more scenarios reaching that same branch. Out-of-bounds is tested because that check is our own code.
