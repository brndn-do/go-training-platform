using System.Data.Common;
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
    try
    {
      return await LoadAsync(id, cancellationToken);
    }
    catch (Exception ex) when (Translate(ex, "Loading a game") is { } failure)
    {
      throw failure;
    }
  }

  /// <inheritdoc/>
  public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
  {
    try
    {
      await context.Games.AddAsync(game, cancellationToken);
      await context.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex) when (Translate(ex, "Adding a game") is { } failure)
    {
      throw failure;
    }
  }

  /// <inheritdoc/>
  public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
  {
    try
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
    catch (Exception ex) when (Translate(ex, "Saving a game") is { } failure)
    {
      throw failure;
    }
  }

  // Translates a store failure into a RepositoryException, or returns null for anything that
  // is not one — the caller's exception filter then leaves it alone rather than reclassifying
  // a programming error or a cancellation as a persistence problem.
  private static RepositoryException? Translate(Exception exception, string operation)
  {
    // Npgsql reports a cancelled command as OperationCanceledException wrapping a Postgres
    // "query_canceled" error, which is not transient. This must stay ahead of the unwrap
    // below, or that error is found there and a disconnected client becomes a store failure.
    if (exception is OperationCanceledException)
    {
      return null;
    }

    // Raised by EF when the concurrency token no longer matches, does not carry a DbException.
    if (exception is DbUpdateConcurrencyException)
    {
      return new RepositoryException(
        RepositoryFailureKind.Conflict,
        $"{operation} failed: the game was changed by someone else.",
        exception);
    }

    // EF wraps a transient failure in an InvalidOperationException but lets a permanent one
    // through untouched, so the real failure sits at either level.
    DbException? storeFailure = exception as DbException ?? exception.InnerException as DbException;
    if (storeFailure is null)
    {
      return null;
    }

    return new RepositoryException(
      storeFailure.IsTransient ? RepositoryFailureKind.Unavailable : RepositoryFailureKind.Rejected,
      $"{operation} failed with SQL state: ({storeFailure.SqlState ?? "no SQLSTATE"}).",
      exception);
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
