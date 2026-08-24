using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// The state of a game after an operation. Returned by every game endpoint.
/// </summary>
/// <param name="Id">The game's id.</param>
/// <param name="PlayerColor">The color the human player is playing as.</param>
/// <param name="BotColor">The color the bot is playing as.</param>
/// <param name="Turn">The color whose turn it is to play.</param>
/// <param name="BoardSize">The width and height of the (square) board.</param>
/// <param name="Komi">The komi for this game.</param>
/// <param name="BotStrength">The strength of the bot for this game.</param>
/// <param name="Outcome"><c>null</c> while the game is in progress; otherwise how it ended.</param>
/// <param name="Board">
/// The board, indexed <c>Board[x][y]</c> — the same order as the domain's coordinates, not
/// row-major. Jagged rather than rectangular because <c>System.Text.Json</c> cannot serialize
/// a multidimensional array.
/// </param>
/// <param name="LastMove">
/// The most recently played move, or <c>null</c> if none has been played. Normally the bot's
/// reply rather than the human's, since the bot answers within the same request.
/// </param>
/// <param name="Suggestion">
/// The engine's hint for the human's next decision, or <c>null</c> when none was requested.
/// </param>
public sealed record GameResponse(
  Guid Id,
  Color PlayerColor,
  Color BotColor,
  Color Turn,
  int BoardSize,
  double Komi,
  BotStrength BotStrength,
  Outcome? Outcome,
  Content[][] Board,
  MoveResponse? LastMove,
  SuggestionResponse? Suggestion)
{
  /// <summary>
  /// Builds a <see cref="GameResponse"/> from a game and an optional hint.
  /// </summary>
  /// <param name="game">The game to represent. Its position must already be built.</param>
  /// <param name="suggestion">The engine's hint, or <c>null</c> if there is none.</param>
  /// <returns>The response representation of <paramref name="game"/>.</returns>
  public static GameResponse From(Game game, EngineSuggestion? suggestion) => new(
    game.Id,
    game.PlayerColor,
    game.BotColor,
    game.Turn,
    game.BoardSize,
    game.Komi,
    game.BotStrength,
    game.Outcome,
    ToJaggedBoard(game.GetBoard()),
    game.Moves.Count == 0 ? null : MoveResponse.From(game.Moves[^1]),
    suggestion is null ? null : SuggestionResponse.From(suggestion));

  private static Content[][] ToJaggedBoard(Content[,] board)
  {
    int size = board.GetLength(0);
    Content[][] jagged = new Content[size][];

    for (int x = 0; x < size; x++)
    {
      jagged[x] = new Content[size];
      for (int y = 0; y < size; y++)
      {
        jagged[x][y] = board[x, y];
      }
    }

    return jagged;
  }
}
