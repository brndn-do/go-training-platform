using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests;

public class GameServiceTests
{
  [Fact]
  public async Task StartGameAsync_ValidInput_CreatesAndPersistsGame()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    Guid playerId = Guid.NewGuid();

    var game = await gameService.StartGameAsync(playerId, Color.Black, 9);

    Assert.Equal(playerId, game.PlayerId);
    Assert.Equal(Color.Black, game.PlayerColor);
    Assert.Equal(9, game.BoardSize);
    Assert.Null(game.Outcome);
    Assert.Equal(Color.Black, game.Turn);

    var persisted = await repository.GetByIdAsync(game.Id);
    Assert.NotNull(persisted);
    Assert.Equivalent(game, persisted);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task StartGameAsync_NonPositiveBoardSize_Throws(int boardSize)
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    Guid playerId = Guid.NewGuid();

    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => gameService.StartGameAsync(playerId, Color.Black, boardSize));
  }
}
