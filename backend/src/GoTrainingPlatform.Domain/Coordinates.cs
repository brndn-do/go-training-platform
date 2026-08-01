namespace GoTrainingPlatform.Domain;

/// <summary>
/// Represents the x, y coordinates of a Go Board.
/// </summary>
public record Coordinates
{
  /// <summary>
  /// Gets the x-coordinate.
  /// </summary>
  public int X { get; init; }

  /// <summary>
  /// Gets the y-coordinate.
  /// </summary>
  public int Y { get; init; }
}