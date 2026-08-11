using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Domain.Tests;

public sealed class GameTests
{
  [Fact]
  public void Constructor_MoveHistory_StoresMovesWithoutReplaying()
  {
    IReadOnlyList<Move> moveHistory = [new Move(new Coordinates(0, 0), 0), new Move(null, 1)];

    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    Assert.Equal(moveHistory, game.Moves);
    Assert.Equal(Color.Black, game.Turn); // still the default — constructor does not replay into the position
  }

  [Fact]
  public void BuildPosition_NonEmptyMoveHistory_ReplaysGame()
  {
    Move move1 = new(new Coordinates(0, 0), 0);
    IReadOnlyList<Move> moveHistory = [move1];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    game.BuildPosition();
    bool success = game.TryRecordMove(Color.White, 0, 0); // 0, 0 occupied if replay happened

    Assert.False(success);
    Assert.Equal(Color.White, game.Turn); // current turn should be white if replay happened
  }

  [Fact]
  public void BuildPosition_MoveHistoryWithOnePass_ReplaysGame()
  {
    IReadOnlyList<Move> moveHistory = [new Move(null, 0)];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    game.BuildPosition();

    Assert.Equal(Color.White, game.Turn);
  }

  [Fact]
  public void BuildPosition_MoveHistoryWithTwoPasses_ReplaysGame()
  {
    IReadOnlyList<Move> moveHistory = [new Move(null, 0), new Move(null, 1)];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    game.BuildPosition();

    Assert.Equal(Color.Black, game.Turn);
  }

  [Fact]
  public void TryRecordMove_UnfinishedGameLegalMove_RecordsAndReturnsTrue()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success = game.TryRecordMove(Color.Black, 0, 0);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Fact]
  public void TryRecordMove_UnfinishedGameIllegalMove_DoesNotRecordAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success1 = game.TryRecordMove(Color.Black, 0, 0);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success1);
    Assert.False(success2);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Fact]
  public void TryRecordMove_WrongColor_DoesNotRecordAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool wrongColorAttempt = game.TryRecordMove(Color.White, 0, 0);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(wrongColorAttempt);
    Assert.Empty(game.Moves);
  }

  [Theory]
  [InlineData(Outcome.BotResigned, false)]
  [InlineData(Outcome.PlayerResigned, false)]
  [InlineData(Outcome.TwoConsecutivePasses, false)]
  [InlineData(Outcome.BotResigned, true)]
  [InlineData(Outcome.PlayerResigned, true)]
  [InlineData(Outcome.TwoConsecutivePasses, true)]
  public void TryRecordMove_FinishedGame_DoesNotRecordAndReturnsFalse(Outcome outcome, bool pointOccupied)
  {
    IReadOnlyList<Move> moveHistory = pointOccupied ? [new Move(new Coordinates(0, 0), 0)] : [];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);
    game.BuildPosition();
    Color movingColor = pointOccupied ? Color.White : Color.Black;

    bool success = game.TryRecordMove(movingColor, 0, 0);

    // for any finished game, should fail and return false regardless of the move's legality
    Assert.Equal(movingColor, game.Turn);
    Assert.False(success);
    Assert.Equal(moveHistory, game.Moves);
  }

  [Fact]
  public void TryRecordPass_UnfinishedGame_PassesAndReturnsTrue()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success = game.TryRecordPass(Color.Black);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success);
    Assert.Equal([new Move(null, 0)], game.Moves);
  }

  [Fact]
  public void TryRecordPass_WrongColor_DoesNotPassAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool wrongColorAttempt = game.TryRecordPass(Color.White);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(wrongColorAttempt);
    Assert.Empty(game.Moves);
  }

  [Theory]
  [InlineData(Outcome.BotResigned, false)]
  [InlineData(Outcome.PlayerResigned, false)]
  [InlineData(Outcome.TwoConsecutivePasses, false)]
  [InlineData(Outcome.BotResigned, true)]
  [InlineData(Outcome.PlayerResigned, true)]
  [InlineData(Outcome.TwoConsecutivePasses, true)]
  public void TryRecordPass_FinishedGame_DoesNotPassAndReturnsFalse(Outcome outcome, bool colorIsWrong)
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);
    game.BuildPosition();

    bool success = game.TryRecordPass(colorIsWrong ? Color.White : Color.Black);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(success);
    Assert.Empty(game.Moves);
  }

  [Fact]
  public void TryRecordPass_TwoConsecutivePasses_SetsCorrectOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordPass(Color.White);

    Assert.True(success1);
    Assert.True(success2);
    Assert.Equal(Outcome.TwoConsecutivePasses, game.Outcome);
    Assert.Equal([new Move(null, 0), new Move(null, 1)], game.Moves);
  }

  [Fact]
  public void TryRecordPass_OnePass_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success1 = game.TryRecordPass(Color.Black);

    Assert.True(success1);
    Assert.Null(game.Outcome);
    Assert.Equal([new Move(null, 0)], game.Moves);
  }

  [Fact]
  public void TryRecordPass_TwoNonConsecutivePassesSamePlayer_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);
    bool success3 = game.TryRecordPass(Color.Black);

    Assert.True(success1);
    Assert.True(success2);
    Assert.True(success3);
    Assert.Null(game.Outcome);
    Assert.Equal([new Move(null, 0), new Move(new Coordinates(0, 0), 1), new Move(null, 2)], game.Moves);
  }

  [Fact]
  public void TryRecordPass_TwoNonConsecutivePassestakeDifferentPlayers_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);
    bool success3 = game.TryRecordMove(Color.Black, 1, 1);
    bool success4 = game.TryRecordPass(Color.White);

    Assert.True(success1);
    Assert.True(success2);
    Assert.True(success3);
    Assert.True(success4);
    Assert.Null(game.Outcome);
    Assert.Equal(
      [new Move(null, 0), new Move(new Coordinates(0, 0), 1), new Move(new Coordinates(1, 1), 2), new Move(null, 3)],
      game.Moves);
  }

  [Theory]
  [InlineData(Color.Black, Outcome.PlayerResigned)]
  [InlineData(Color.White, Outcome.BotResigned)]
  public void TryRecordResign_UnfinishedGame_ResignsAndReturnsTrue(Color resigningColor, Outcome expectedOutcome)
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success = game.TryRecordResign(resigningColor);

    Assert.Equal(expectedOutcome, game.Outcome);
    Assert.True(success);
    Assert.Empty(game.Moves); // resigning is not itself recorded as a Move
  }

  [Theory]
  [InlineData(Outcome.BotResigned)]
  [InlineData(Outcome.PlayerResigned)]
  [InlineData(Outcome.TwoConsecutivePasses)]
  public void TryRecordResign_FinishedGame_DoesNotResignAndReturnsFalse(Outcome outcome)
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);
    game.BuildPosition();

    bool success = game.TryRecordResign(Color.Black);

    Assert.Equal(outcome, game.Outcome);
    Assert.False(success);
    Assert.Empty(game.Moves);
  }

  [Fact]
  public void TryUndo_LastMoveIsHumanMove_UndoesOnlyThatMove()
  {
    // player is white, who made the second move
    Game game = new([new Move(new Coordinates(0, 0), 0), new Move(new Coordinates(1, 1), 1)], Guid.NewGuid(), Guid.NewGuid(), Color.White, 9, null);
    game.BuildPosition();

    bool success = game.TryUndo();

    Assert.True(success);
    Assert.Equal(game.PlayerColor, game.Turn);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Fact]
  public void TryUndo_LastMoveIsBotResponse_UndoesTwoMoves()
  {
    // player is white, bot move, followed by player move, followed by bot move
    Game game = new([new Move(new Coordinates(0, 0), 0), new Move(new Coordinates(1, 1), 1), new Move(new Coordinates(2, 2), 2)], Guid.NewGuid(), Guid.NewGuid(), Color.White, 9, null);
    game.BuildPosition();

    bool success = game.TryUndo();

    Assert.True(success);
    Assert.Equal(game.PlayerColor, game.Turn);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Fact]
  public void TryUndo_PlayerIsBlackAndNoMoves_ReturnsFalse()
  {
    // player is black, no moves
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    bool success = game.TryUndo();

    Assert.False(success);
    Assert.Empty(game.Moves);
  }

  [Fact]
  public void TryUndo_PlayerIsWhiteAndNoWhiteMoves_ReturnsFalse()
  {
    // player is white, no white moves
    Game game = new([new Move(new Coordinates(0, 0), 0)], Guid.NewGuid(), Guid.NewGuid(), Color.White, 9, null);
    game.BuildPosition();

    bool success = game.TryUndo();

    Assert.False(success);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Theory]
  [InlineData(Outcome.BotResigned)]
  [InlineData(Outcome.PlayerResigned)]
  [InlineData(Outcome.TwoConsecutivePasses)]
  public void TryUndo_FinishedGame_ReturnsFalse(Outcome outcome)
  {
    // A human move exists in history here, so the only reason this can fail
    // is the finished-game guard
    Game game = new([new Move(new Coordinates(0, 0), 0)], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);
    game.BuildPosition();

    bool success = game.TryUndo();

    Assert.False(success);
    Assert.Equal([new Move(new Coordinates(0, 0), 0)], game.Moves);
  }

  [Fact]
  public void Turn_PositionNotBuilt_Throws()
  {
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Throws<InvalidOperationException>(() => game.Turn);
  }

  [Fact]
  public void GetBoard_PositionNotBuilt_Throws()
  {
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Throws<InvalidOperationException>(() => game.GetBoard());
  }

  [Fact]
  public void TryRecordMove_PositionNotBuilt_Throws()
  {
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Throws<InvalidOperationException>(() => game.TryRecordMove(Color.Black, 0, 0));
  }

  [Fact]
  public void TryRecordPass_PositionNotBuilt_Throws()
  {
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Throws<InvalidOperationException>(() => game.TryRecordPass(Color.Black));
  }

  [Theory]
  [InlineData(null)]
  [InlineData(Outcome.BotResigned)]
  public void TryUndo_PositionNotBuilt_Throws(Outcome? outcome)
  {
    // Unlike TryRecordMove/TryRecordPass, TryUndo reads Turn before checking
    // Outcome, so it throws even for a finished game.
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);

    Assert.Throws<InvalidOperationException>(() => game.TryUndo());
  }

  [Fact]
  public void TryRecordResign_PositionNotBuilt_DoesNotThrow()
  {
    // TryRecordResign never reads the position — resigning isn't turn-gated —
    // so it's the one method that's still safe to call before BuildPosition().
    Game game = new(Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success = game.TryRecordResign(Color.Black);

    Assert.True(success);
  }
}
