using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application;

/// <summary>
/// Orchestrates use cases for <see cref="Game"/>, coordinating domain logic with persistence.
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
}
