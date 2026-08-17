namespace GoTrainingPlatform.Infrastructure.Engine;

/// <summary>
/// A request for a suggested move and win rate for a given position.
/// </summary>
/// <param name="Moves">
/// The move history, in order, each either a coordinate or <c>null</c> for a pass.
/// </param>
/// <param name="BoardSize">The width and height of the (square) board.</param>
/// <param name="Komi">The game's komi.</param>
/// <param name="BotStrength">The bot strength formatted as Kyu20, Dan9, Superhuman, etc.</param>
internal sealed record SuggestionRequest(
  IReadOnlyList<Move?> Moves,
  int BoardSize,
  double Komi,
  string BotStrength);
