using GoTrainingPlatform.Application;
using GoTrainingPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IGameRepository"/>.
/// </summary>
public class GameRepository(GoTrainingPlatformDbContext context) : IGameRepository
{
  /// <inheritdoc/>
  public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await context.Games.FindAsync([id], cancellationToken);
  }

  /// <inheritdoc/>
  public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
  {
    await context.Games.AddAsync(game, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
  {
    var existing = await context.Games.FindAsync([game.Id], cancellationToken);

    if (existing is null)
    {
      return;
    }

    context.Entry(existing).CurrentValues.SetValues(game);

    var trackedMoves = (ICollection<Move>)context.Entry(existing).Collection(nameof(Game.Moves)).CurrentValue!;

    HashSet<Move> movesInExisting = [.. existing.Moves];
    HashSet<Move> movesInGame = [.. game.Moves];

    List<Move> removed = [.. movesInExisting.Except(movesInGame)];
    List<Move> added = [.. movesInGame.Except(movesInExisting)];

    foreach (var move in removed)
    {
      trackedMoves.Remove(move);
    }

    foreach (var move in added)
    {
      trackedMoves.Add(move);
    }

    await context.SaveChangesAsync(cancellationToken);
  }
}
