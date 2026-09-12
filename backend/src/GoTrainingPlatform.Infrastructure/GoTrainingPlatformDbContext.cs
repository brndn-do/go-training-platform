using GoTrainingPlatform.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core context for the platform's persisted state.
/// </summary>
public sealed class GoTrainingPlatformDbContext(DbContextOptions<GoTrainingPlatformDbContext> options)
  : IdentityUserContext<ApplicationUser, Guid>(options) // IdentityUserContext, not IdentityDbContext. No roles exist today.
{
  /// <summary>
  /// Gets the set of persisted games.
  /// </summary>
  public DbSet<Game> Games => Set<Game>();

  /// <inheritdoc/>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // call IdentityUserContext's version to set up Identity tables/configuration first
    base.OnModelCreating(modelBuilder);

    // Identity's own OnModelCreating calls ToTable(...) with PascalCase names, which
    // UseSnakeCaseNamingConvention() does not rewrite. Rename explicitly to match the rest
    // of the schema.
    modelBuilder.Entity<ApplicationUser>().ToTable("users");
    modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
    modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
    modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

    // build ours on top
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoTrainingPlatformDbContext).Assembly);
  }
}
