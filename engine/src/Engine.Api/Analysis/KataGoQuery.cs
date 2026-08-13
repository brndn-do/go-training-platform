using System.Text.Json.Serialization;
using static Engine.Api.Extensions.DoubleExtensions;

namespace Engine.Api.Analysis;

/// <summary>
/// A query to KataGo's JSON analysis engine.
/// </summary>
public sealed record KataGoQuery
{
  private const string DefaultRules = "chinese";

  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoQuery"/> class.
  /// </summary>
  /// <param name="id">The query's id, echoed back in the response.</param>
  /// <param name="moveHistory">
  /// The move history, in order, each either (x, y) board coordinates or <c>null</c> for a pass.
  /// </param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="komi">The game's komi.</param>
  /// <param name="botStrength">The bot strength.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="boardSize"/> is not between 2 and 19, or when
  /// <paramref name="komi"/> is not between -150 and 150.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="komi"/> is not an integer or half-integer.
  /// </exception>
  public KataGoQuery(
    string id,
    IReadOnlyList<Move?> moveHistory,
    int boardSize,
    double komi,
    BotStrength botStrength)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(boardSize, 2);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(boardSize, 19);
    ArgumentOutOfRangeException.ThrowIfLessThan(komi, -150);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(komi, 150);

    if (!IsIntegerOrHalfInteger(komi))
    {
      throw new ArgumentException("Komi must be an integer or half integer.", nameof(komi));
    }

    BoardXSize = boardSize;
    BoardYSize = boardSize;
    Komi = komi;
    Id = id;
    Moves = KataGoMoveConverter.Convert(moveHistory);
    Rules = DefaultRules;
    IncludePolicy = true;
    OverrideSettings = botStrength.IsSuperhuman ? null : new KataGoOverrideSettings(botStrength);
  }

  /// <summary>
  /// Gets the query's id, echoed back in the response.
  /// </summary>
  public string Id { get; }

  /// <summary>
  /// Gets the move history, in KataGo's [color, move] pair format.
  /// </summary>
  public IReadOnlyList<string[]> Moves { get; }

  /// <summary>
  /// Gets the ruleset. Always "chinese".
  /// </summary>
  public string Rules { get; }

  /// <summary>
  /// Gets the game's komi.
  /// </summary>
  public double Komi { get; }

  /// <summary>
  /// Gets the width of the board.
  /// </summary>
  public int BoardXSize { get; }

  /// <summary>
  /// Gets the height of the board.
  /// </summary>
  public int BoardYSize { get; }

  /// <summary>
  /// Gets a value indicating whether to include the policy/humanPolicy fields in the response.
  /// Always <c>true</c>.
  /// </summary>
  public bool IncludePolicy { get; }

  /// <summary>
  /// Gets the override settings, or <c>null</c> for Superhuman strength. Omitted from the
  /// serialized JSON when <c>null</c> — KataGo's analysis engine rejects an explicit
  /// <c>"overrideSettings":null</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public KataGoOverrideSettings? OverrideSettings { get; }
}