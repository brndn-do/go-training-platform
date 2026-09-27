using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// One entry in the current player's game list. Carries no board: opening a game is a separate
/// read of <see cref="GameResponse"/>.
/// </summary>
/// <param name="Id">The game's id.</param>
/// <param name="PlayerColor">The color the human player is playing as.</param>
/// <param name="BotColor">The color the bot is playing as.</param>
/// <param name="BoardSize">The width and height of the (square) board.</param>
/// <param name="Komi">The komi for this game.</param>
/// <param name="BotStrength">The strength of the bot for this game.</param>
/// <param name="Outcome"><c>null</c> while the game is in progress; otherwise how it ended.</param>
/// <param name="MoveCount">The number of moves played, passes included.</param>
/// <param name="CreatedAt">When the game was started.</param>
/// <param name="UpdatedAt">When the game last changed.</param>
public sealed record GameSummaryResponse(
  Guid Id,
  Color PlayerColor,
  Color BotColor,
  int BoardSize,
  double Komi,
  BotStrength BotStrength,
  Outcome? Outcome,
  int MoveCount,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt)
{
  /// <summary>
  /// Builds a <see cref="GameSummaryResponse"/> from a game's summary.
  /// </summary>
  /// <param name="summary">The summary to represent.</param>
  /// <returns>The response representation of <paramref name="summary"/>.</returns>
  public static GameSummaryResponse From(GameSummary summary) => new(
    summary.Id,
    summary.PlayerColor,
    summary.PlayerColor == Color.Black ? Color.White : Color.Black,
    summary.BoardSize,
    summary.Komi,
    summary.BotStrength,
    summary.Outcome,
    summary.MoveCount,
    summary.CreatedAt,
    summary.UpdatedAt);
}
