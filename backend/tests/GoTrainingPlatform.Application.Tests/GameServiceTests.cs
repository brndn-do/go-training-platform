using GoTrainingPlatform.Domain;
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

  [Fact]
  public async Task LoadGameAsync_ExistingGame_ReturnsGameWithPositionBuilt()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    Guid gameId = Guid.NewGuid();

    // do not call BuildPosition after creating game. Because of our fake, in-memory repository,
    // repository.addAsync followed by gameService.LoadGameAsync will just retrieve the same
    // reference, and we want to test that LoadGameAsync correctly builds the position itself.
    Game game = new([new Move(new Coordinates(0, 0), 0)], gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    var result = await gameService.LoadGameAsync(gameId);

    Assert.NotNull(result);
    Assert.Equivalent(game, result);
    Assert.Equal(Color.White, result.Turn); // check position was built
  }

  [Fact]
  public async Task LoadGameAsync_NonExistentId_ReturnsNull()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    var result = await gameService.LoadGameAsync(Guid.NewGuid());

    Assert.Null(result);
  }
}
