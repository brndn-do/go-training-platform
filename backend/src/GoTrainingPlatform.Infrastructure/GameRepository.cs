using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IGameRepository"/>.
/// </summary>
public sealed class GameRepository(GoTrainingPlatformDbContext context) : IGameRepository
{
  /// <inheritdoc/>
  public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await LoadAsync(id, cancellationToken);
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
    var existing = await LoadAsync(game.Id, cancellationToken);

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

  // Both load paths go through here: EF fills a navigation once, so whichever call materializes
  // a game first decides its move order for the rest of the context's life.
  private async Task<Game?> LoadAsync(Guid id, CancellationToken cancellationToken)
  {
    // A query by id can't use the context's tracked entities the way FindAsync does, so it
    // would re-fetch the whole move history on every call. Anything already tracked came from
    // this method or from AddAsync, so it is already ordered.
    Game? tracked = context.Games.Local.FirstOrDefault(game => game.Id == id);
    if (tracked is not null)
    {
      return tracked;
    }

    return await context.Games
      .Include(game => game.Moves.OrderBy(move => move.MoveNumber))
      .FirstOrDefaultAsync(game => game.Id == id, cancellationToken);
  }
}
