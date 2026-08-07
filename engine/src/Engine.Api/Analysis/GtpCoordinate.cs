namespace Engine.Api.Analysis;

/// <summary>
/// Converts between board coordinates and GTP coordinate strings, as used by KataGo's move format.
/// </summary>
public static class GtpCoordinate
{
  private static readonly IReadOnlyList<char> _columns =
    ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T'];

  // Derived from _columns, not hand-written, so the letters/order are only ever defined once.
  private static readonly Dictionary<char, int> _columnIndices =
    _columns.Select((column, index) => (column, index)).ToDictionary(pair => pair.column, pair => pair.index);

  /// <summary>
  /// Converts board coordinates into a GTP coordinate string for use with KataGo.
  /// </summary>
  /// <param name="x">The x-coordinate, 0-indexed.</param>
  /// <param name="y">The y-coordinate, 0-indexed.</param>
  /// <returns>The GTP coordinate string.</returns>
  public static string ToGtp(int x, int y)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(x, 0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(x, 18);
    ArgumentOutOfRangeException.ThrowIfLessThan(y, 0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(y, 18);
    return $"{_columns[x]}{y + 1}";
  }

  /// <summary>
  /// Converts a GTP coordinate string into board coordinates.
  /// </summary>
  /// <param name="gtp">The GTP coordinate string, e.g. "A1" or "T19".</param>
  /// <returns>The 0-indexed (x, y) board coordinates.</returns>
  public static (int X, int Y) FromGtp(string gtp)
  {
    if (string.IsNullOrEmpty(gtp))
    {
      throw new FormatException("GTP coordinate string must not be null or empty.");
    }

    char column = gtp[0];
    if (!_columnIndices.TryGetValue(column, out int x))
    {
      throw new FormatException($"'{column}' is not a valid GTP column.");
    }

    string rowPart = gtp[1..];
    if (!int.TryParse(rowPart, out int row) || row < 1 || row > 19)
    {
      throw new FormatException($"'{rowPart}' is not a valid GTP row number.");
    }

    return (x, row - 1);
  }
}