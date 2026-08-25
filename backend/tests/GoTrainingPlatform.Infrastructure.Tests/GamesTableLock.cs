using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// Holds an exclusive lock on the games table until disposed, so a query on any other
/// connection blocks instead of returning. Disposing rolls the transaction back.
/// </summary>
public sealed class GamesTableLock : IAsyncDisposable
{
  private readonly GoTrainingPlatformDbContext _context;
  private readonly IAsyncDisposable _transaction;

  private GamesTableLock(GoTrainingPlatformDbContext context, IAsyncDisposable transaction)
  {
    _context = context;
    _transaction = transaction;
  }

  /// <summary>
  /// Takes the lock on its own connection.
  /// </summary>
  /// <param name="postgresFixture">The fixture supplying the connection.</param>
  /// <returns>The held lock, released when disposed.</returns>
  public static async Task<GamesTableLock> AcquireAsync(PostgresFixture postgresFixture)
  {
    var context = postgresFixture.CreateContext();
    var transaction = await context.Database.BeginTransactionAsync();
    await context.Database.ExecuteSqlRawAsync("LOCK TABLE games IN ACCESS EXCLUSIVE MODE");

    return new GamesTableLock(context, transaction);
  }

  /// <inheritdoc/>
  public async ValueTask DisposeAsync()
  {
    await _transaction.DisposeAsync();
    await _context.DisposeAsync();
  }
}
