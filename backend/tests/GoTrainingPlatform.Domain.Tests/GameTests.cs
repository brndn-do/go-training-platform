using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Domain.Tests;

public class GameTests
{
  [Fact]
  public void Constructor_NonEmptyMoveHistory_ReplaysGame()
  {
    Move move1 = new(new Coordinates(0, 0));
    IReadOnlyList<Move> moveHistory = [move1];

    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);
    bool success = game.TryRecordMove(Color.White, 0, 0); // 0, 0 occupied if replay happened

    Assert.False(success);
    Assert.Equal(Color.White, game.Turn); // current turn should be white if replay happend
  }

  [Fact]
  public void Constructor_MoveHistoryWithOnePass_ReplaysGame()
  {
    IReadOnlyList<Move> moveHistory = [new Move(null)];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Equal(Color.White, game.Turn);
  }

  [Fact]
  public void Constructor_MoveHistoryWithTwoPasses_ReplaysGame()
  {
    IReadOnlyList<Move> moveHistory = [new Move(null), new Move(null)];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    Assert.Equal(Color.Black, game.Turn);
  }

  [Fact]
  public void TryRecordMove_UnfinishedGameLegalMove_RecordsAndReturnsTrue()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success = game.TryRecordMove(Color.Black, 0, 0);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success);
  }

  [Fact]
  public void TryRecordMove_UnfinishedGameIllegalMove_DoesNotRecordAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordMove(Color.Black, 0, 0);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success1);
    Assert.False(success2);
  }

  [Fact]
  public void TryRecordMove_WrongColor_DoesNotRecordAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool wrongColorAttempt = game.TryRecordMove(Color.White, 0, 0);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(wrongColorAttempt);
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
    IReadOnlyList<Move> moveHistory = pointOccupied ? [new Move(new Coordinates(0, 0))] : [];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);
    Color movingColor = pointOccupied ? Color.White : Color.Black;

    bool success = game.TryRecordMove(movingColor, 0, 0);

    // for any finished game, should fail and return false regardless of the move's legality
    Assert.Equal(movingColor, game.Turn);
    Assert.False(success);
  }

  [Fact]
  public void TryRecordPass_UnfinishedGame_PassesAndReturnsTrue()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success = game.TryRecordPass(Color.Black);

    Assert.Equal(Color.White, game.Turn);
    Assert.True(success);
  }

  [Fact]
  public void TryRecordPass_WrongColor_DoesNotPassAndReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool wrongColorAttempt = game.TryRecordPass(Color.White);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(wrongColorAttempt);
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

    bool success = game.TryRecordPass(colorIsWrong ? Color.White : Color.Black);

    Assert.Equal(Color.Black, game.Turn);
    Assert.False(success);
  }

  [Fact]
  public void TryRecordPass_TwoConsecutivePasses_SetsCorrectOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordPass(Color.White);

    Assert.True(success1);
    Assert.True(success2);
    Assert.Equal(Outcome.TwoConsecutivePasses, game.Outcome);
  }

  [Fact]
  public void TryRecordPass_OnePass_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordPass(Color.Black);

    Assert.True(success1);
    Assert.Null(game.Outcome);
  }

  [Fact]
  public void TryRecordPass_TwoNonConsecutivePassesSamePlayer_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);
    bool success3 = game.TryRecordPass(Color.Black);

    Assert.True(success1);
    Assert.True(success2);
    Assert.True(success3);
    Assert.Null(game.Outcome);
  }

  [Fact]
  public void TryRecordPass_TwoNonConsecutivePassestakeDifferentPlayers_DoesNotSetOutcome()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordPass(Color.Black);
    bool success2 = game.TryRecordMove(Color.White, 0, 0);
    bool success3 = game.TryRecordMove(Color.Black, 1, 1);
    bool success4 = game.TryRecordPass(Color.White);

    Assert.True(success1);
    Assert.True(success2);
    Assert.True(success3);
    Assert.True(success4);
    Assert.Null(game.Outcome);
  }

  [Theory]
  [InlineData(Color.Black, Outcome.PlayerResigned)]
  [InlineData(Color.White, Outcome.BotResigned)]
  public void TryRecordResign_UnfinishedGame_ResignsAndReturnsTrue(Color resigningColor, Outcome expectedOutcome)
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success = game.TryRecordResign(resigningColor);

    Assert.Equal(expectedOutcome, game.Outcome);
    Assert.True(success);
  }

  [Theory]
  [InlineData(Outcome.BotResigned)]
  [InlineData(Outcome.PlayerResigned)]
  [InlineData(Outcome.TwoConsecutivePasses)]
  public void TryRecordResign_FinishedGame_DoesNotResignAndReturnsFalse(Outcome outcome)
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);

    bool success = game.TryRecordResign(Color.Black);

    Assert.Equal(outcome, game.Outcome);
    Assert.False(success);
  }
}