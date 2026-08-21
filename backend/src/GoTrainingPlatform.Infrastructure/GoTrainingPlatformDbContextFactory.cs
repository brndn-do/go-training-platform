using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// Design-time factory the <c>dotnet ef</c> CLI tooling uses to construct a
/// <see cref="GoTrainingPlatformDbContext"/> for commands like
/// <c>migrations add</c>/<c>database update</c>, when there is no running
/// application/DI container to resolve one from. Never invoked at runtime —
/// the real API wires up <see cref="GoTrainingPlatformDbContext"/> via its own
/// DI registration. Reads <c>ConnectionStrings__DefaultConnection</c> from the
/// environment, so migrations run against the same database the app uses.
/// </summary>
public sealed class GoTrainingPlatformDbContextFactory : IDesignTimeDbContextFactory<GoTrainingPlatformDbContext>
{
  private const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";

  /// <summary>
  /// Builds the <see cref="GoTrainingPlatformDbContext"/> the <c>dotnet ef</c>
  /// tooling uses at design time.
  /// </summary>
  /// <param name="args">Command-line arguments passed by the EF Core tooling; unused here.</param>
  /// <returns>A context configured against <c>ConnectionStrings__DefaultConnection</c>.</returns>
  /// <exception cref="InvalidOperationException">
  /// If <c>ConnectionStrings__DefaultConnection</c> is not set.
  /// </exception>
  public GoTrainingPlatformDbContext CreateDbContext(string[] args)
  {
    string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException(
        $"{ConnectionStringVariable} is not set. Export it first: "
        + "set -a && source .env && set +a");
    }

    var optionsBuilder = new DbContextOptionsBuilder<GoTrainingPlatformDbContext>();
    optionsBuilder
      .UseNpgsql(connectionString)
      .UseSnakeCaseNamingConvention();
    return new GoTrainingPlatformDbContext(optionsBuilder.Options);
  }
}
