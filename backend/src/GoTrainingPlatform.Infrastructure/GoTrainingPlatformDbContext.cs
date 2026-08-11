using GoTrainingPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core context for the platform's persisted state.
/// </summary>
public sealed class GoTrainingPlatformDbContext(DbContextOptions<GoTrainingPlatformDbContext> options)
  : DbContext(options)
{
  /// <summary>
  /// Gets the set of persisted games.
  /// </summary>
  public DbSet<Game> Games => Set<Game>();

  /// <inheritdoc/>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoTrainingPlatformDbContext).Assembly);
  }
}
