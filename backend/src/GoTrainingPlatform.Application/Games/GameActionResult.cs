using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application.Games;

/// <summary>
/// The result of a <see cref="GameService"/> game-action method (make a move, pass, undo, resign).
/// </summary>
/// <param name="Game">
/// The affected game, or <c>null</c> only when no game with the given id exists. Present for
/// every other outcome (including a failed action), since the game was already loaded by then.
/// </param>
/// <param name="Success">
/// <c>true</c> if the underlying <c>Game</c> method accepted the action and it was persisted;
/// <c>false</c> if the game did not exist, or the domain rejected the action for any reason
/// (wrong turn, illegal move, or the game had already finished) — the game itself guarantees
/// nothing changed in that case, so this type does not distinguish which reason applied.
/// </param>
public sealed record GameActionResult(Game? Game, bool Success);
