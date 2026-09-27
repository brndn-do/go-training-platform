using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Games;

/// <summary>
/// One row of a player's game list: enough to pick a game to open, without its move history.
/// Read straight from the store rather than built from a <see cref="Game"/>, since building one
/// would replay every move to reach a board nobody asked for.
/// </summary>
/// <param name="Id">The game's id.</param>
/// <param name="PlayerColor">The color the human player is playing as.</param>
/// <param name="BoardSize">The width and height of the (square) board.</param>
/// <param name="Komi">The komi for this game.</param>
/// <param name="BotStrength">The strength of the bot for this game.</param>
/// <param name="Outcome"><c>null</c> while the game is in progress; otherwise how it ended.</param>
/// <param name="MoveCount">The number of moves played, passes included.</param>
/// <param name="CreatedAt">When the game was started.</param>
/// <param name="UpdatedAt">When the game last changed. Equal to <paramref name="CreatedAt"/> until then.</param>
public sealed record GameSummary(
  Guid Id,
  Color PlayerColor,
  int BoardSize,
  double Komi,
  BotStrength BotStrength,
  Outcome? Outcome,
  int MoveCount,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt);
