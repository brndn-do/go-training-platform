using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Domain;

/// <summary>
/// The aggregate root for a single game: the player, board size, move history, and
/// outcome (once finished). Owns a <see cref="GoPosition"/> internally, reconstructed
/// by replaying move history, to enforce Go-rules legality and finished-game
/// invariants on every recorded move.
/// </summary>
public class Game
{
  private readonly List<Move> moves;
  private readonly GoPosition goPosition;

  /// <summary>
  /// Initializes a new instance of the <see cref="Game"/> class, either as a brand-new
  /// game (empty <paramref name="moveHistory"/>) or reconstructed from persisted state
  /// by replaying <paramref name="moveHistory"/> to rebuild the current position.
  /// </summary>
  /// <param name="moveHistory">The full, ordered move history to replay.</param>
  /// <param name="id">The game's id.</param>
  /// <param name="playerId">The id of the human player.</param>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="outcome"><c>null</c> if still in progress; otherwise how the game ended.</param>
  /// <param name="komi">The komi to use for this game and to report to the underlying rules engine.</param>
  public Game(IReadOnlyList<Move> moveHistory, Guid id, Guid playerId, Color playerColor, int boardSize, Outcome? outcome, double komi = 7.5)
  {
    moves = [.. moveHistory];
    goPosition = new GoPosition(boardSize, komi);
    Id = id;
    PlayerId = playerId;
    PlayerColor = playerColor;
    BoardSize = boardSize;
    Komi = komi;
    Outcome = outcome;

    foreach (var move in moves)
    {
      if (move.Coordinates is null)
      {
        goPosition.Pass();
      }
      else
      {
        goPosition.TryMakeMove(move.Coordinates.X, move.Coordinates.Y);
      }
    }
  }

  /// <summary>
  /// Gets the game's id.
  /// </summary>
  public Guid Id { get; }

  /// <summary>
  /// Gets the id of the human player.
  /// </summary>
  public Guid PlayerId { get; }

  /// <summary>
  /// Gets the color the human player is playing as.
  /// </summary>
  public Color PlayerColor { get; }

  /// <summary>
  /// Gets the color of the player whose turn it is to play.
  /// </summary>
  public Color Turn => ToColor(goPosition.Turn);

  /// <summary>
  /// Gets the size of the board where size = width = height.
  /// </summary>
  public int BoardSize { get; }

  /// <summary>
  /// Gets the komi for this game.
  /// </summary>
  public double Komi { get; }

  // TODO: bot strength, decide difficulty representation (e.g. easy, normal, hard) or configurable visit/playout

  /// <summary>
  /// Gets how the game ended, or <c>null</c> if it's still in progress.
  /// </summary>
  public Outcome? Outcome { get; private set; }

  /// <summary>
  /// Gets the current board state.
  /// </summary>
  /// <returns>
  /// A 2D rectangular array of size <see cref="BoardSize"/> x <see cref="BoardSize"/>,
  /// indexed the same way as <see cref="TryRecordMove(Color, int, int)"/>.
  /// </returns>
  public Content[,] GetBoard() => goPosition.GetBoard();

  /// <summary>
  /// Attempts to record a stone placement at the given coordinates for
  /// <paramref name="movingColor"/>.
  /// </summary>
  /// <param name="movingColor">The color of the player or bot attempting to move.</param>
  /// <param name="x">The X coordinate of the move, zero-indexed.</param>
  /// <param name="y">The Y coordinate of the move, zero-indexed.</param>
  /// <returns>
  /// <c>true</c> if the game was still in progress, it was <paramref name="movingColor"/>'s
  /// turn, and the move was legal, in which case it has been recorded; <c>false</c>
  /// otherwise, in which case nothing changed.
  /// </returns>
  public bool TryRecordMove(Color movingColor, int x, int y)
  {
    if (Outcome is not null)
    {
      return false;
    }

    if (movingColor != Turn)
    {
      return false;
    }

    bool legal = goPosition.TryMakeMove(x, y);
    if (!legal)
    {
      return false;
    }

    Coordinates coord = new(x, y);
    Move move = new(coord);
    moves.Add(move);
    return true;
  }

  /// <summary>
  /// Attempts to record a pass for <paramref name="passingColor"/>.
  /// </summary>
  /// <param name="passingColor">The color of the player or bot attempting to pass.</param>
  /// <returns>
  /// <c>true</c> if the game was still in progress and it was <paramref name="passingColor"/>'s
  /// turn, in which case the pass has been recorded; <c>false</c> otherwise.
  /// </returns>
  public bool TryRecordPass(Color passingColor)
  {
    if (Outcome is not null)
    {
      return false;
    }

    if (passingColor != Turn)
    {
      return false;
    }

    goPosition.Pass();

    moves.Add(new Move(null));
    return true;
  }

  /// <summary>
  /// Attempts to resign the game for <paramref name="resigningColor"/>.
  /// </summary>
  /// <param name="resigningColor">
  /// The color resigning — compared against <see cref="PlayerColor"/>, not <see cref="Turn"/>,
  /// since resigning is not turn-gated.
  /// </param>
  /// <returns>
  /// <c>true</c> if the game was still in progress, in which case the resignation has
  /// been recorded; <c>false</c> otherwise.
  /// </returns>
  public bool TryRecordResign(Color resigningColor)
  {
    if (Outcome is not null)
    {
      return false;
    }

    Outcome = resigningColor == PlayerColor ? Enums.Outcome.PlayerResigned : Enums.Outcome.BotResigned;
    return true;
  }

  private static Color ToColor(Content content) => content switch
  {
    Content.Black => Color.Black,
    Content.White => Color.White,
    _ => throw new ArgumentOutOfRangeException(nameof(content)),
  };
}
