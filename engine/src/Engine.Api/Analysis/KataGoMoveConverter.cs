namespace Engine.Api.Analysis;

/// <summary>
/// Converts a move history into KataGo's "moves" query field format.
/// </summary>
public static class KataGoMoveConverter
{
  /// <summary>
  /// Converts a move history into KataGo's "moves" query field format: a list of
  /// [color, move] pairs, where color alternates starting with Black and move is either a GTP
  /// coordinate string or "pass".
  /// </summary>
  /// <param name="moves">
  /// The move history in order, each either (x, y) board coordinates or <c>null</c> for a pass.
  /// </param>
  /// <returns>The moves in KataGo's query format.</returns>
  public static IReadOnlyList<string[]> Convert(IReadOnlyList<(int X, int Y)?> moves)
  {
    string[][] result = new string[moves.Count][];

    for (int i = 0; i < moves.Count; i++)
    {
      string color = i % 2 == 0 ? "B" : "W";
      string move = moves[i] is (int x, int y) ? GtpCoordinate.ToGtp(x, y) : "pass";
      result[i] = [color, move];
    }

    return result;
  }
}
