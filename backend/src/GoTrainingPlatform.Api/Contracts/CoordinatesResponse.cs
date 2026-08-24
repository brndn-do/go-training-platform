namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A point on the board, zero-indexed.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
public sealed record CoordinatesResponse(int X, int Y);
