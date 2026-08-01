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
    bool success = game.TryRecordMove(0, 0); // 0, 0 occupied if replay happened

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
  public void TryRecordMove_UnfinishedGameLegalMove_ReturnsTrue()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success = game.TryRecordMove(0, 0);

    Assert.True(success);
  }

  [Fact]
  public void TryRecordMove_UnfinishedGameIllegalMove_ReturnsFalse()
  {
    Game game = new([], Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, null);

    bool success1 = game.TryRecordMove(0, 0);
    bool success2 = game.TryRecordMove(0, 0);

    Assert.True(success1);
    Assert.False(success2);
  }

  [Theory]
  [InlineData(Outcome.BotResigned, false)]
  [InlineData(Outcome.PlayerResigned, false)]
  [InlineData(Outcome.TwoConsecutivePasses, false)]
  [InlineData(Outcome.BotResigned, true)]
  [InlineData(Outcome.PlayerResigned, true)]
  [InlineData(Outcome.TwoConsecutivePasses, true)]
  public void TryRecordMove_FinishedGame_ReturnsFalse(Outcome outcome, bool pointOccupied)
  {
    IReadOnlyList<Move> moveHistory = pointOccupied ? [new Move(new Coordinates(0, 0))] : [];
    Game game = new(moveHistory, Guid.NewGuid(), Guid.NewGuid(), Color.Black, 9, outcome);

    bool success = game.TryRecordMove(0, 0);

    // for any finished game, should return false regardless of the move's legality
    Assert.False(success);
  }
}