using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// Design-time factory the <c>dotnet ef</c> CLI tooling uses to construct a
/// <see cref="GoTrainingPlatformDbContext"/> for commands like
/// <c>migrations add</c>/<c>database update</c>, when there is no running
/// application/DI container to resolve one from. Never invoked at runtime —
/// the real API wires up <see cref="GoTrainingPlatformDbContext"/> via its own
/// DI registration, reading the connection string from configuration instead
/// of the hardcoded one below.
/// </summary>
public sealed class GoTrainingPlatformDbContextFactory : IDesignTimeDbContextFactory<GoTrainingPlatformDbContext>
{
  /// <summary>
  /// Builds the <see cref="GoTrainingPlatformDbContext"/> the <c>dotnet ef</c>
  /// tooling uses at design time.
  /// </summary>
  /// <param name="args">Command-line arguments passed by the EF Core tooling; unused here.</param>
  /// <returns>A context configured against the local dev Postgres instance.</returns>
  public GoTrainingPlatformDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<GoTrainingPlatformDbContext>();
    optionsBuilder
      .UseNpgsql("Host=localhost;Port=5432;Database=gotraining;Username=gotraining;Password=changeme")
      .UseSnakeCaseNamingConvention();
    return new GoTrainingPlatformDbContext(optionsBuilder.Options);
  }
}
