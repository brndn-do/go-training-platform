namespace Engine.Api.Analysis;

/// <summary>
/// Gets a suggested move and win rate for a position by querying KataGo and interpreting its
/// response.
/// </summary>
public sealed class SuggestionService(IKataGoClient kataGoClient, Random random)
{
  /// <summary>
  /// Gets a suggested move and win rate for the position resulting from replaying
  /// <paramref name="moveHistory"/> in order.
  /// </summary>
  /// <param name="moveHistory">
  /// The move history, in order, each either (x, y) board coordinates or <c>null</c> for a pass.
  /// </param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="komi">The game's komi.</param>
  /// <param name="botStrength">The bot strength formatted as Kyu20, Dan9, Superhuman, etc.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The suggested move (<c>null</c> for pass) and the resulting win rate.</returns>
  public async Task<((int X, int Y)? Coordinates, double Winrate)> GetSuggestionAsync(
    IReadOnlyList<(int X, int Y)?> moveHistory,
    int boardSize,
    double komi,
    string botStrength,
    CancellationToken cancellationToken = default)
  {
    KataGoQuery query = new(Guid.NewGuid().ToString(), moveHistory, boardSize, komi, botStrength);
    KataGoResponse response = await kataGoClient.QueryAsync(query, cancellationToken);
    return KataGoResponseInterpreter.Interpret(response, botStrength, random);
  }
}
