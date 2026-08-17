namespace GoTrainingPlatform.Infrastructure.Engine;

/// <summary>
/// A single board coordinate in a move history or suggestion response.
/// </summary>
/// <param name="X">The x-coordinate, 0-indexed.</param>
/// <param name="Y">The y-coordinate, 0-indexed.</param>
internal sealed record Move(int X, int Y);
