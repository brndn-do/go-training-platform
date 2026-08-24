using System.ComponentModel.DataAnnotations;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A request to start a new game.
/// </summary>
public sealed record StartGameRequest
{
  /// <summary>
  /// Gets the color the human player plays as. Required.
  /// </summary>
  [Required]
  public Color? PlayerColor { get; init; }

  /// <summary>
  /// Gets the width and height of the (square) board. Required.
  /// </summary>
  [Required]
  [AllowedValues(9, 13, 19, ErrorMessage = "BoardSize must be 9, 13, or 19.")]
  public int? BoardSize { get; init; }

  /// <summary>
  /// Gets the strength of the bot to play against. Required.
  /// </summary>
  [Required]
  public BotStrength? BotStrength { get; init; }

  /// <summary>
  /// Gets the komi for this game. Defaults to 7.5 when omitted.
  /// </summary>
  public double Komi { get; init; } = 7.5;
}
