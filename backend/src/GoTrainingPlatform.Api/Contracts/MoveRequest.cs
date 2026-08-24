using System.ComponentModel.DataAnnotations;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A request to place a stone. Both coordinates are required: an omitted coordinate
/// would otherwise bind to zero and silently play a different, legal move.
/// </summary>
public sealed record MoveRequest
{
  /// <summary>
  /// Gets the X coordinate of the move, zero-indexed. Required.
  /// </summary>
  [Required]
  [Range(0, 18)]
  public int? X { get; init; }

  /// <summary>
  /// Gets the Y coordinate of the move, zero-indexed. Required.
  /// </summary>
  [Required]
  [Range(0, 18)]
  public int? Y { get; init; }
}
