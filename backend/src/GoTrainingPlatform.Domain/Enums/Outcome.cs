namespace GoTrainingPlatform.Domain.Enums;

/// <summary>
/// Represents how and why a finished game ended. <c>null</c> on <see cref="Game.Outcome"/>
/// means the game is still in progress.
/// </summary>
public enum Outcome
{
  /// <summary>The human player resigned; the bot wins.</summary>
  PlayerResigned,

  /// <summary>The bot resigned (e.g. at a sufficiently low win-rate); the player wins.</summary>
  BotResigned,

  /// <summary>
  /// Both players passed consecutively. v1 does no automated scoring, so this ends the
  /// game without the app determining a winner.
  /// </summary>
  TwoConsecutivePasses,
}
