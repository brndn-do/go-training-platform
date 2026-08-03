using GoTrainingPlatform.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="Game"/>.
/// </summary>
public class GameConfiguration : IEntityTypeConfiguration<Game>
{
  /// <inheritdoc/>
  public void Configure(EntityTypeBuilder<Game> builder)
  {
    builder.HasKey(game => game.Id);
    builder.Property(game => game.PlayerId);
    builder.Property(game => game.PlayerColor);
    builder.Property(game => game.BoardSize);
    builder.Property(game => game.Outcome);
    builder.Property(game => game.Komi);

    builder.OwnsMany(game => game.Moves, move =>
    {
      move.ToTable("moves");
      move.WithOwner().HasForeignKey("GameId");
      move.HasKey("GameId", nameof(Move.MoveNumber));
      move.Property(m => m.MoveNumber).ValueGeneratedNever();
      move.OwnsOne(m => m.Coordinates);
    });
  }
}
