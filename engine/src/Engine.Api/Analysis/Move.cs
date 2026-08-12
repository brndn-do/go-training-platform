namespace Engine.Api.Analysis;

/// <summary>
/// A single board coordinate in a move history or suggestion response.
/// </summary>
/// <param name="X">The x-coordinate, 0-indexed.</param>
/// <param name="Y">The y-coordinate, 0-indexed.</param>
public sealed record Move(int X, int Y);
