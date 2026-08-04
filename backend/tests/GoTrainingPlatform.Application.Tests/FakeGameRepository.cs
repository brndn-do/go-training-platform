using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application.Tests;

/// <summary>
/// In-memory <see cref="IGameRepository"/>, used to test <see cref="GameService"/>'s
/// own orchestration logic in isolation.
/// </summary>
public class FakeGameRepository : IGameRepository
{
  private readonly Dictionary<Guid, Game> games = [];

  /// <summary>
  /// Gets the number of times <see cref="SaveAsync"/> was called. Since this fake stores
  /// object references directly, a domain mutation (e.g. <c>Game.TryRecordMove</c>) is
  /// visible via <see cref="GetByIdAsync"/> whether or not <see cref="SaveAsync"/> was ever
  /// called — this counter exists so tests can assert persistence actually happened, rather
  /// than a state check that would pass regardless.
  /// </summary>
  public int SaveAsyncCallCount { get; private set; }

  /// <inheritdoc/>
  public Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    games.TryGetValue(id, out var game);
    return Task.FromResult(game);
  }

  /// <inheritdoc/>
  public Task AddAsync(Game game, CancellationToken cancellationToken = default)
  {
    games[game.Id] = game;
    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public Task SaveAsync(Game game, CancellationToken cancellationToken = default)
  {
    games[game.Id] = game;
    SaveAsyncCallCount++;
    return Task.CompletedTask;
  }
}
