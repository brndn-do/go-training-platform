using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A move that was played.
/// </summary>
/// <param name="Coordinates">
/// The point played, or <c>null</c> if the move was a pass. A pass is not visible on the
/// board, so this is the only way to tell one from a game that has not started.
/// </param>
/// <param name="MoveNumber">The move's zero-based position in the game's history.</param>
public sealed record MoveResponse(CoordinatesResponse? Coordinates, int MoveNumber)
{
  /// <summary>
  /// Builds a <see cref="MoveResponse"/> from a domain move.
  /// </summary>
  /// <param name="move">The move to convert.</param>
  /// <returns>The response representation of <paramref name="move"/>.</returns>
  public static MoveResponse From(Move move) => new(
    move.Coordinates is null
      ? null
      : new CoordinatesResponse(move.Coordinates.X, move.Coordinates.Y),
    move.MoveNumber);
}
