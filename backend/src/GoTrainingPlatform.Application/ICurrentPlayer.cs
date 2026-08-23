namespace GoTrainingPlatform.Application;

/// <summary>
/// Supplies the ID of the player making the current request. Used by <see cref="Games.GameService"/>.
/// </summary>
public interface ICurrentPlayer
{
  /// <summary>
  /// Gets the ID of the current player.
  /// </summary>
  Guid Id { get; }
}