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
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="botStrength"/> is neither "Superhuman" nor a valid Kyu/Dan
  /// format (bubbles up from <see cref="BotStrength"/>'s constructor), or when
  /// <paramref name="boardSize"/> is not between 2 and 19, or <paramref name="komi"/> is not
  /// between -150 and 150 (bubbles up from <see cref="KataGoQuery"/>'s constructor).
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="komi"/> is not an integer or half-integer. Bubbles up from
  /// <see cref="KataGoQuery"/>'s constructor.
  /// </exception>
  /// <exception cref="InvalidKataGoResponseException">
  /// Thrown when KataGo's response is an error response, or its policy, win rate, or policy
  /// length is invalid for the requested <paramref name="botStrength"/>. Bubbles up from
  /// <see cref="KataGoResponseInterpreter.Interpret(KataGoResponse, BotStrength, Random)"/>.
  /// </exception>
  public async Task<SuggestionResponse> GetSuggestionAsync(
    IReadOnlyList<Move?> moveHistory,
    int boardSize,
    double komi,
    string botStrength,
    CancellationToken cancellationToken = default)
  {
    BotStrength strength = new(botStrength);
    KataGoQuery query = new(Guid.NewGuid().ToString(), moveHistory, boardSize, komi, strength);
    KataGoResponse response = await kataGoClient.QueryAsync(query, cancellationToken);
    var (move, winrate) = KataGoResponseInterpreter.Interpret(response, strength, random);
    return new SuggestionResponse(move, winrate);
  }
}
