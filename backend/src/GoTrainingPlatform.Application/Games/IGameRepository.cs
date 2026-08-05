using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application.Games;

/// <summary>
/// Persistence abstraction for <see cref="Game"/>, implemented by Infrastructure.
/// </summary>
public interface IGameRepository
{
  /// <summary>
  /// Loads a game by id. The returned <see cref="Game"/> has not had
  /// <see cref="Game.BuildPosition"/> called — callers must call it before
  /// trusting <see cref="Game.Turn"/> or <see cref="Game.GetBoard"/>.
  /// </summary>
  /// <param name="id">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game, or <c>null</c> if no game with that id exists.</returns>
  Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists a brand-new game that has never been saved before.
  /// </summary>
  /// <param name="game">The game to persist.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task AddAsync(Game game, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists changes to an existing game.
  /// </summary>
  /// <param name="game">The game to persist.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
