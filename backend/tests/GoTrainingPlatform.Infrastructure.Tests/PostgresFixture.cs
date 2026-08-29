using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

  /// <summary>
  /// Gets the id of a user seeded into this fixture's database. A game's <c>PlayerId</c> is a
  /// foreign key onto the user table, so persisting one requires an id that exists there.
  /// </summary>
  public Guid PlayerId { get; } = Guid.NewGuid();

  /// <inheritdoc/>
  public async Task InitializeAsync()
  {
    await _container.StartAsync();

    await using var context = CreateContext();
    await context.Database.MigrateAsync();

    context.Users.Add(new ApplicationUser { Id = PlayerId, UserName = "integration-tests" });
    await context.SaveChangesAsync();
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
  public GoTrainingPlatformDbContext CreateContext() => CreateContext(_container.GetConnectionString());

  /// <summary>
  /// Creates a context pointed at a port nothing is listening on, so connecting fails as a
  /// transient network error rather than reaching a server at all. A refused connection on
  /// loopback returns immediately, unlike an unroutable address, which waits out the timeout.
  /// </summary>
  /// <returns>The database context.</returns>
  public GoTrainingPlatformDbContext CreateUnreachableContext()
  {
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    int unusedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();

    NpgsqlConnectionStringBuilder connectionString = new(_container.GetConnectionString())
    {
      Host = "127.0.0.1",
      Port = unusedPort,
      Timeout = 2,
    };

    return CreateContext(connectionString.ConnectionString);
  }

  /// <summary>
  /// Creates a context pointed at this fixture's real server but at a database that does not
  /// exist, so the server itself refuses the connection with a permanent error.
  /// </summary>
  /// <returns>The database context.</returns>
  public GoTrainingPlatformDbContext CreateMissingDatabaseContext()
  {
    NpgsqlConnectionStringBuilder connectionString = new(_container.GetConnectionString())
    {
      Database = "no_such_database",
    };

    return CreateContext(connectionString.ConnectionString);
  }

  private static GoTrainingPlatformDbContext CreateContext(string connectionString)
  {
    var options = new DbContextOptionsBuilder<GoTrainingPlatformDbContext>()
      .UseNpgsql(connectionString)
      .UseSnakeCaseNamingConvention()
      .Options;

    return new GoTrainingPlatformDbContext(options);
  }
}
