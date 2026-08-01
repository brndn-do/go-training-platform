namespace GoTrainingPlatform.Domain;

/// <summary>
/// Represents a move.
/// </summary>
public record Move
{
  /// <summary>
  /// Gets the coordinates of the move, null if the move is a pass.
  /// </summary>
  public Coordinates? Coordinates { get; init; }
}