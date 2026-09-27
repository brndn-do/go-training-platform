using GoTrainingPlatform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="Game"/>.
/// </summary>
public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
  /// <summary>
  /// The shadow property recording when a game was created.
  /// </summary>
  public const string CreatedAt = nameof(CreatedAt);

  /// <summary>
  /// The shadow property recording when a game last changed.
  /// </summary>
  public const string UpdatedAt = nameof(UpdatedAt);

  /// <inheritdoc/>
  public void Configure(EntityTypeBuilder<Game> builder)
  {
    builder.HasKey(game => game.Id);
    builder.Property(game => game.PlayerId);
    builder.Property(game => game.PlayerColor);
    builder.Property(game => game.BoardSize);
    builder.Property(game => game.Outcome);
    builder.Property(game => game.Komi);
    builder.Property(game => game.BotStrength);

    // Shadow properties: when a game was saved is a fact about its storage, not a Go rule, so
    // the domain never sees it. GameRepository stamps both. The defaults only backfill rows that
    // predate these columns.
    builder.Property<DateTimeOffset>(CreatedAt).HasDefaultValueSql("now()");
    builder.Property<DateTimeOffset>(UpdatedAt).HasDefaultValueSql("now()");

    // Serves ListByPlayerAsync's filter and order in one scan, and covers the foreign key.
    builder.HasIndex(nameof(Game.PlayerId), UpdatedAt);

    builder.Property<uint>("xmin")
      .HasColumnName("xmin")
      .IsRowVersion(); // note: as of EF Core 7 UseXminAsConcurrencyToken is no longer used

    builder.OwnsMany(game => game.Moves, move =>
    {
      move.ToTable("moves");
      move.WithOwner().HasForeignKey("GameId");
      move.HasKey("GameId", nameof(Move.MoveNumber));
      move.Property(m => m.MoveNumber).ValueGeneratedNever();
      move.OwnsOne(m => m.Coordinates);
    });

    builder.HasOne<ApplicationUser>()
      .WithMany()
      .HasForeignKey(game => game.PlayerId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
