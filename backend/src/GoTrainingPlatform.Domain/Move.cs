namespace GoTrainingPlatform.Domain;

/// <summary>
/// Represents a move.
/// </summary>
public record Move
{
  /// <summary>
  /// Initializes a new instance of the <see cref="Move"/> class representing a pass
  /// (<see cref="Coordinates"/> stays <c>null</c>).
  /// </summary>
  /// <remarks>
  /// This constructor's parameter is scalar-only so EF Core can bind through it.
  /// </remarks>
  /// <param name="moveNumber">The move's position in the game, zero-indexed.</param>
  public Move(int moveNumber)
  {
    MoveNumber = moveNumber;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Move"/> class representing a stone
  /// placement, or a pass if <paramref name="coordinates"/> is <c>null</c>.
  /// </summary>
  /// <param name="coordinates">The coordinates played, or <c>null</c> for a pass.</param>
  /// <param name="moveNumber">The move's position in the game, zero-indexed.</param>
  public Move(Coordinates? coordinates, int moveNumber)
    : this(moveNumber)
  {
    Coordinates = coordinates;
  }

  /// <summary>
  /// Gets the coordinates played, or <c>null</c> if this move was a pass.
  /// </summary>
  public Coordinates? Coordinates { get; init; }

  /// <summary>
  /// Gets the move's position in the game, zero-indexed.
  /// </summary>
  public int MoveNumber { get; }
}