using GoSharp = GoSharpCore;

namespace GoTrainingPlatform.Domain;

public class Game
{
  // Standard komi for Chinese ruleset
  private const double Komi = 7.5;
  private GoSharp.Game goSharpGame;
  private readonly int boardSize;

  public Game(int boardSize)
  {
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(boardSize, 0);
    GoSharp.GameInfo gi = new() { Komi = Komi, BoardSizeX = boardSize, BoardSizeY = boardSize };
    goSharpGame = new(gi);
    this.boardSize = boardSize;
  }

  /// <summary>
  /// Attempts to place a stone at the given coordinates for the player whose turn it is.
  /// </summary>
  /// <param name="x">The X coordinate of the move, zero-indexed.</param>
  /// <param name="y">The Y coordinate of the move, zero-indexed.</param>
  /// <returns>
  /// <c>true</c> if the move was legal and has been applied; <c>false</c> if it was
  /// illegal (out of bounds, occupied, suicide, or a ko violation), in which case the
  /// game's state is left completely unchanged.
  /// </returns>
  public bool TryMakeMove(int x, int y)
  {
    // check for out of bounds as GoSharp throws if out of bounds
    if (x < 0 || y < 0 || x >= boardSize || y >= boardSize)
    {
      return false;
    }

    var nextState = goSharpGame.MakeMove(x, y, out bool legal);
    if (!legal)
    {
      return false;
    }
    goSharpGame = nextState;
    return true;
  }

  /// <summary>
  /// Gets the state of the board, with each coordinate being <c>Empty</c>, <c>Black</c>, or <c>White</c>.
  /// </summary>
  /// <returns>
  /// A 2D rectangular array of size boardSize x boardSize representing a snapshot of the current board state,
  /// with coordinates indexed the same way as <c>TryMakeMove</c>, with three possible values: <c>Empty</c>, <c>Black</c>, or <c>White</c>.
  /// </returns>
  public Content[,] GetBoard()
  {
    Content[,] board = new Content[boardSize, boardSize];
    for (int x = 0; x < boardSize; x++)
    {
      for (int y = 0; y < boardSize; y++)
      {
        board[x, y] = ToContent(goSharpGame.Board.GetContentAt(x, y));
      }
    }
    return board;
  }

  private static Content ToContent(GoSharp.Content content) => content switch
  {
    GoSharp.Content.Empty => Content.Empty,
    GoSharp.Content.Black => Content.Black,
    GoSharp.Content.White => Content.White,
    _ => throw new ArgumentOutOfRangeException(nameof(content)),
  };
}
