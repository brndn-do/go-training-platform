using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Domain;

/// <summary>
/// The aggregate root for a single game: the player, board size, move history, and
/// outcome (once finished). Owns a <see cref="GoPosition"/> internally to enforce
/// Go-rules legality and finished-game invariants on every recorded move.
/// Every constructor leaves the internal position empty — if <see cref="Moves"/>
/// was populated from existing history (via the move-history constructor overload,
/// or by EF Core loading a persisted game), call <see cref="BuildPosition"/>
/// explicitly afterward, before trusting <see cref="Turn"/> or <see cref="GetBoard"/>.
/// </summary>
public class Game
{
  private List<Move> moves = [];
  private GoPosition? goPosition;

  /// <summary>
  /// Initializes a new instance of the <see cref="Game"/> class.
  /// </summary>
  /// <param name="id">The game's id.</param>
  /// <param name="playerId">The id of the human player.</param>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="outcome"><c>null</c> if still in progress; otherwise how the game ended.</param>
  /// <param name="komi">The komi to use for this game and to report to the underlying rules engine.</param>
  /// <param name="botStrength">The strength of the bot for this game.</param>
  #pragma warning disable IDE0290 // Rich aggregate with EF Core constructor-binding history — not a fit for a primary constructor.
  public Game(Guid id, Guid playerId, Color playerColor, int boardSize, Outcome? outcome, double komi = 7.5, BotStrength botStrength = BotStrength.Superhuman)
  {
    Id = id;
    PlayerId = playerId;
    PlayerColor = playerColor;
    BoardSize = boardSize;
    Komi = komi;
    Outcome = outcome;
    BotStrength = botStrength;
  }
  #pragma warning restore IDE0290

  /// <summary>
  /// Initializes a new instance of the <see cref="Game"/> class with existing move
  /// history already populated in <see cref="Moves"/>. Does not replay
  /// <paramref name="moveHistory"/> into the position — call <see cref="BuildPosition"/>
  /// explicitly afterward, before trusting <see cref="Turn"/> or <see cref="GetBoard"/>.
  /// </summary>
  /// <param name="moveHistory">The full, ordered move history to store.</param>
  /// <param name="id">The game's id.</param>
  /// <param name="playerId">The id of the human player.</param>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="outcome"><c>null</c> if still in progress; otherwise how the game ended.</param>
  /// <param name="komi">The komi to use for this game and to report to the underlying rules engine.</param>
  /// <param name="botStrength">The strength of the bot for this game.</param>
  public Game(IReadOnlyList<Move> moveHistory, Guid id, Guid playerId, Color playerColor, int boardSize, Outcome? outcome, double komi = 7.5, BotStrength botStrength = BotStrength.Superhuman)
    : this(id, playerId, playerColor, boardSize, outcome, komi, botStrength)
  {
    moves = [.. moveHistory];
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
  public Color Turn => ToColor(RequirePosition().Turn);

  /// <summary>
  /// Gets the size of the board where size = width = height.
  /// </summary>
  public int BoardSize { get; }

  /// <summary>
  /// Gets the komi for this game.
  /// </summary>
  public double Komi { get; }

  /// <summary>
  /// Gets the strength of the bot for this game.
  /// </summary>
  public BotStrength BotStrength { get; }

  /// <summary>
  /// Gets how the game ended, or <c>null</c> if it's still in progress.
  /// </summary>
  public Outcome? Outcome { get; private set; }

  /// <summary>
  /// Gets the full, ordered move history.
  /// </summary>
  public IReadOnlyList<Move> Moves => moves;

  /// <summary>
  /// Gets the current board state.
  /// </summary>
  /// <returns>
  /// A 2D rectangular array of size <see cref="BoardSize"/> x <see cref="BoardSize"/>,
  /// indexed the same way as <see cref="TryRecordMove(Color, int, int)"/>.
  /// </returns>
  public Content[,] GetBoard() => RequirePosition().GetBoard();

  /// <summary>
  /// Rebuilds the internal <see cref="GoPosition"/> from scratch by replaying every
  /// entry in <see cref="Moves"/>, in order. Must be called explicitly whenever
  /// <see cref="Moves"/> was populated from existing history rather than built up via
  /// <see cref="TryRecordMove"/>/<see cref="TryRecordPass"/> — this includes the
  /// move-history constructor overload and EF Core's own materialization when
  /// loading a persisted game, neither of which triggers a replay on its own.
  /// </summary>
  public void BuildPosition()
  {
    goPosition = new GoPosition(BoardSize, Komi);

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

    bool legal = RequirePosition().TryMakeMove(x, y);
    if (!legal)
    {
      return false;
    }

    Coordinates coord = new(x, y);
    Move move = new(coord, moves.Count);
    moves.Add(move);
    return true;
  }

  /// <summary>
  /// Attempts to record a pass for <paramref name="passingColor"/>.
  /// If successful and this is the second consecutive pass, ends the game and sets the outcome to <c>TwoConsecutivePasses</c>.
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

    RequirePosition().Pass();
    moves.Add(new Move(null, moves.Count));

    if (moves.Count >= 2 && moves.TakeLast(2).All(move => move.Coordinates is null))
    {
      Outcome = Enums.Outcome.TwoConsecutivePasses;
    }

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

  /// <summary>
  /// Attempts to undo back to before the human player's most recent move — trimming
  /// that move and, if the bot had already responded to it, the bot's response too,
  /// then rebuilding the position via <see cref="BuildPosition"/>. Mutates this
  /// instance in place. Fails for a finished game.
  /// </summary>
  /// <returns>
  /// <c>true</c> if the game was still in progress and a human move existed in
  /// history to undo, in which case the trim and rebuild have been applied;
  /// <c>false</c> otherwise, in which case nothing changed.
  /// </returns>
  public bool TryUndo()
  {
    int undoCount = Turn == PlayerColor ? 2 : 1;

    if (Outcome is not null || moves.Count < undoCount)
    {
      return false;
    }

    moves = [.. moves.SkipLast(undoCount)];

    BuildPosition();

    return true;
  }

  private static Color ToColor(Content content) => content switch
  {
    Content.Black => Color.Black,
    Content.White => Color.White,
    _ => throw new ArgumentOutOfRangeException(nameof(content)),
  };

  private GoPosition RequirePosition() =>
    goPosition ?? throw new InvalidOperationException("Game.BuildPosition was never called.");
}