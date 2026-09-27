using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application.Tests.Games;

/// <summary>
/// In-memory <see cref="IGameRepository"/>, used to test <see cref="GameService"/>'s
/// own orchestration logic in isolation.
/// </summary>
public sealed class FakeGameRepository : IGameRepository
{
  private readonly Dictionary<Guid, Game> _games = [];
  private readonly Dictionary<Guid, (DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)> _times = [];

  // Advances on every write, so each one is strictly later than the last without a real clock.
  private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
    _games.TryGetValue(id, out var game);
    return Task.FromResult(game);
  }

  /// <inheritdoc/>
  public Task<IReadOnlyList<GameSummary>> ListByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
  {
    IReadOnlyList<GameSummary> summaries = [.. _games.Values
      .Where(game => game.PlayerId == playerId)
      .Select(game => new GameSummary(
        game.Id,
        game.PlayerColor,
        game.BoardSize,
        game.Komi,
        game.BotStrength,
        game.Outcome,
        game.Moves.Count,
        _times[game.Id].CreatedAt,
        _times[game.Id].UpdatedAt))
      .OrderByDescending(summary => summary.UpdatedAt)
      .ThenBy(summary => summary.Id)];

    return Task.FromResult(summaries);
  }

  /// <inheritdoc/>
  public Task AddAsync(Game game, CancellationToken cancellationToken = default)
  {
    _games[game.Id] = game;
    DateTimeOffset now = Tick();
    _times[game.Id] = (now, now);
    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public Task SaveAsync(Game game, CancellationToken cancellationToken = default)
  {
    _games[game.Id] = game;
    _times[game.Id] = (_times[game.Id].CreatedAt, Tick());
    SaveAsyncCallCount++;
    return Task.CompletedTask;
  }

  private DateTimeOffset Tick() => _now = _now.AddSeconds(1);
}
