using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application.Tests;

/// <summary>
/// In-memory <see cref="IGameRepository"/>, used to test <see cref="GameService"/>'s
/// own orchestration logic in isolation.
/// </summary>
public class FakeGameRepository : IGameRepository
{
  private readonly Dictionary<Guid, Game> games = [];

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
    return Task.CompletedTask;
  }
}
