using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// Spins up a real, throwaway Postgres container for the lifetime of a test
/// collection, migrated once via <see cref="GoTrainingPlatformDbContext"/>'s
/// real EF Core migrations — not a mock or an in-memory provider.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
  private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

  /// <inheritdoc/>
  public async Task InitializeAsync()
  {
    await _container.StartAsync();

    await using var context = CreateContext();
    await context.Database.MigrateAsync();
  }

  /// <inheritdoc/>
  public async Task DisposeAsync()
  {
    await _container.DisposeAsync();
  }

  /// <summary>
  /// Creates a new <see cref="GoTrainingPlatformDbContext"/> pointed at this
  /// fixture's container. Callers get a fresh context per call.
  /// </summary>
  /// <returns>
  /// The database context.
  /// </returns>
  public GoTrainingPlatformDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<GoTrainingPlatformDbContext>()
      .UseNpgsql(_container.GetConnectionString())
      .UseSnakeCaseNamingConvention()
      .Options;

    return new GoTrainingPlatformDbContext(options);
  }
}
