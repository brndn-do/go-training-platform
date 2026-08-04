using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application;

/// <summary>
/// Persists <see cref="Game"/>'s use cases: start, load, and the four game-actions (move, pass,
/// undo, resign). Deliberately thin — every rule check (turn order, legality, whether the game
/// has finished, resignation semantics) is delegated to <see cref="Game"/>'s own <c>Try*</c>
/// methods, and this class persists only on success. It has no knowledge of who's calling it
/// (human or bot) or whether they're authorized to act as the given color; a separate
/// orchestrator, is responsible for that sequencing and verification.
/// </summary>
public class GameService(IGameRepository gameRepository)
{
  /// <summary>
  /// Starts a new game for a human player and persists it.
  /// </summary>
  /// <param name="playerId">The id of the human player.</param>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="komi">The komi to use for this game.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The newly created, persisted <see cref="Game"/>, with its position already built.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="boardSize"/> is not positive.
  /// </exception>
  public async Task<Game> StartGameAsync(
    Guid playerId,
    Color playerColor,
    int boardSize,
    double komi = 7.5,
    CancellationToken cancellationToken = default)
  {
    Game game = new(Guid.NewGuid(), playerId, playerColor, boardSize, null, komi);
    game.BuildPosition();

    await gameRepository.AddAsync(game, cancellationToken);

    return game;
  }

  /// <summary>
  /// Loads a game and builds its position, without mutating it.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game, with its position already built, or <c>null</c> if no game with that id exists.</returns>
  public async Task<Game?> LoadGameAsync(Guid gameId, CancellationToken cancellationToken = default)
  {
    Game? game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
    game?.BuildPosition();
    return game;
  }

  /// <summary>
  /// Attempts to record a stone placement for <paramref name="movingColor"/> and persists it
  /// on success. Does not check turn order, board legality, or whether the game has finished
  /// itself — <see cref="Game.TryRecordMove"/> already owns those checks, and this method just
  /// reports whether it accepted the move.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="movingColor">The color of the player or bot attempting to move.</param>
  /// <param name="x">The X coordinate of the move, zero-indexed.</param>
  /// <param name="y">The Y coordinate of the move, zero-indexed.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the loaded
  /// game, with <c>Success</c> reflecting whether the move was recorded and persisted.
  /// </returns>
  public async Task<GameActionResult> MakeMoveAsync(
    Guid gameId,
    Color movingColor,
    int x,
    int y,
    CancellationToken cancellationToken = default)
  {
    Game? game = await LoadGameAsync(gameId, cancellationToken);
    if (game is null)
    {
      return new GameActionResult(null, false);
    }

    bool success = game.TryRecordMove(movingColor, x, y);
    if (success)
    {
      await gameRepository.SaveAsync(game, cancellationToken);
    }

    return new GameActionResult(game, success);
  }

  /// <summary>
  /// Attempts to record a pass for <paramref name="passingColor"/> and persists it on success.
  /// Does not check turn order or whether the game has finished itself —
  /// <see cref="Game.TryRecordPass"/> already owns those checks (and may itself end the game on
  /// two consecutive passes); this method just reports whether it accepted the pass.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="passingColor">The color of the player or bot attempting to pass.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the loaded
  /// game, with <c>Success</c> reflecting whether the pass was recorded and persisted.
  /// </returns>
  public async Task<GameActionResult> MakePassAsync(
    Guid gameId,
    Color passingColor,
    CancellationToken cancellationToken = default)
  {
    Game? game = await LoadGameAsync(gameId, cancellationToken);
    if (game is null)
    {
      return new GameActionResult(null, false);
    }

    bool success = game.TryRecordPass(passingColor);
    if (success)
    {
      await gameRepository.SaveAsync(game, cancellationToken);
    }

    return new GameActionResult(game, success);
  }

  /// <summary>
  /// Attempts to undo back to before the human player's most recent move and persists it on
  /// success. Does not check whether the game has finished or whether there's anything to
  /// undo itself — <see cref="Game.TryUndo"/> already owns those checks; this method just
  /// reports whether it accepted the undo.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the loaded
  /// game, with <c>Success</c> reflecting whether the undo was applied and persisted.
  /// </returns>
  public async Task<GameActionResult> UndoAsync(Guid gameId, CancellationToken cancellationToken = default)
  {
    Game? game = await LoadGameAsync(gameId, cancellationToken);
    if (game is null)
    {
      return new GameActionResult(null, false);
    }

    bool success = game.TryUndo();
    if (success)
    {
      await gameRepository.SaveAsync(game, cancellationToken);
    }

    return new GameActionResult(game, success);
  }

  /// <summary>
  /// Attempts to resign the game for <paramref name="resigningColor"/> and persists it on
  /// success. Does not check whether the game has finished itself, and is not turn-gated —
  /// <see cref="Game.TryRecordResign"/> already owns those checks; this method just reports
  /// whether it accepted the resignation.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="resigningColor">The color resigning.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the loaded
  /// game, with <c>Success</c> reflecting whether the resignation was recorded and persisted.
  /// </returns>
  public async Task<GameActionResult> ResignAsync(
    Guid gameId,
    Color resigningColor,
    CancellationToken cancellationToken = default)
  {
    Game? game = await LoadGameAsync(gameId, cancellationToken);
    if (game is null)
    {
      return new GameActionResult(null, false);
    }

    bool success = game.TryRecordResign(resigningColor);
    if (success)
    {
      await gameRepository.SaveAsync(game, cancellationToken);
    }

    return new GameActionResult(game, success);
  }
}
