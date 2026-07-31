namespace GoTrainingPlatform.Domain.Tests;

public class GoPositionTests
{
  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_NonPositiveBoardSize_ThrowsArgumentOutOfRangeException(int boardSize)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new GoPosition(boardSize));
  }

  [Fact]
  public void TryMakeMove_LegalMove_ReturnsTrue()
  {
    var game = new GoPosition(9);

    bool legal = game.TryMakeMove(5, 5);

    Assert.True(legal);
  }

  [Fact]
  public void TryMakeMove_CornerPoint_ReturnsTrue()
  {
    var game = new GoPosition(9);

    bool legal = game.TryMakeMove(0, 0);

    Assert.True(legal);
  }

  [Fact]
  public void TryMakeMove_OutOfBounds_ReturnsFalse()
  {
    var game = new GoPosition(9);

    bool legal = game.TryMakeMove(9, 9);

    Assert.False(legal);
  }

  // Test that the class continuously remembers board size
  [Fact]
  public void TryMakeMove_OutOfBoundAfterOneMove_ReturnsFalse()
  {
    var game = new GoPosition(9);

    game.TryMakeMove(0, 0);
    bool legal = game.TryMakeMove(9, 9);

    Assert.False(legal);
  }

  [Fact]
  public void TryMakeMove_TwoLegalMoves_ReturnsTrue()
  {
    var game = new GoPosition(9);

    bool firstLegal = game.TryMakeMove(0, 0);
    bool secondLegal = game.TryMakeMove(8, 8);

    Assert.True(firstLegal);
    Assert.True(secondLegal);
  }

  [Fact]
  public void TryMakeMove_OccupiedPoint_ReturnsFalse()
  {
    var game = new GoPosition(9);

    game.TryMakeMove(0, 0);
    bool legal = game.TryMakeMove(0, 0);

    Assert.False(legal);
  }

  [Fact]
  public void GetBoard_Empty_ReturnsCorrectBoard()
  {
    var game = new GoPosition(9);

    var board = game.GetBoard();

    Assert.Equal(9 * 9, board.Length);

    bool allEmpty = board.Cast<Content>().All(x => x == Content.Empty);
    Assert.True(allEmpty);
  }

  [Fact]
  public void GetBoard_AfterLegalMoves_ReturnsCorrectBoard()
  {
    var game = new GoPosition(9);

    bool legal1 = game.TryMakeMove(0, 0);
    bool legal2 = game.TryMakeMove(8, 8);

    var board = game.GetBoard();
    var enumerable = board.Cast<Content>();

    Assert.True(legal1);
    Assert.True(legal2);
    Assert.Equal(9 * 9, board.Length);
    Assert.Equal(Content.Black, enumerable.First());
    Assert.Equal(Content.White, enumerable.Last());
    foreach (var content in enumerable.Skip(1).Take(enumerable.Count() - 2))
    {
      Assert.Equal(Content.Empty, content);
    }
  }

  [Fact]
  public void GetBoard_AfterIllegalMoves_ReturnsCorrectBoard()
  {
    var game = new GoPosition(9);

    bool legal1 = game.TryMakeMove(0, 0);
    bool legal2 = game.TryMakeMove(0, 0);
    bool legal3 = game.TryMakeMove(9, 9);

    var board = game.GetBoard();
    var enumerable = board.Cast<Content>();

    Assert.True(legal1);
    Assert.False(legal2);
    Assert.False(legal3);
    Assert.Equal(9 * 9, board.Length);
    Assert.Equal(Content.Black, enumerable.First());
    foreach (var content in enumerable.Skip(1))
    {
      Assert.Equal(Content.Empty, content);
    }
  }

  [Fact]
  public void Pass_AlternatesTurnWithoutChangingBoard()
  {
    var game = new GoPosition(9);

    var turn1 = game.Turn;
    var board1 = game.GetBoard();

    game.Pass();

    var turn2 = game.Turn;
    var board2 = game.GetBoard();

    Assert.NotEqual(turn1, turn2);

    Assert.NotSame(board1, board2);

    var enumerable1 = board1.Cast<Content>();
    var enumerable2 = board2.Cast<Content>();

    Assert.Equal(enumerable1, enumerable2);
  }
}