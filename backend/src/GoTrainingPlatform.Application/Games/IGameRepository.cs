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
  /// trusting <see cref="Game.Turn"/> or <see cref="Game.GetBoard"/>. The returned
  /// moves are sorted ascending by move order.
  /// </summary>
  /// <param name="id">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game, or <c>null</c> if no game with that id exists.</returns>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>) or
  /// refuses the read (<see cref="RepositoryFailureKind.Rejected"/>).
  /// </exception>
  Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Lists every game a player owns, finished or not, most recently changed first.
  /// </summary>
  /// <param name="playerId">The id of the player whose games to list.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A summary of each game, empty if the player has none.</returns>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>) or
  /// refuses the read (<see cref="RepositoryFailureKind.Rejected"/>).
  /// </exception>
  Task<IReadOnlyList<GameSummary>> ListByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists a brand-new game that has never been saved before, recording when it was created.
  /// </summary>
  /// <param name="game">The game to persist.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>) or
  /// refuses the write (<see cref="RepositoryFailureKind.Rejected"/>).
  /// </exception>
  Task AddAsync(Game game, CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists changes to an existing game, recording when it last changed.
  /// </summary>
  /// <param name="game">The game to persist.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="RepositoryException">
  /// If the game was changed by someone else since it was loaded
  /// (<see cref="RepositoryFailureKind.Conflict"/>), the store cannot be reached
  /// (<see cref="RepositoryFailureKind.Unavailable"/>), or the store refuses the write
  /// (<see cref="RepositoryFailureKind.Rejected"/>).
  /// </exception>
  Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
