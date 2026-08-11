using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests.Games;

public sealed class GameServiceTests
{
  [Fact]
  public async Task StartGameAsync_ValidInput_CreatesAndPersistsGame()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    Guid playerId = Guid.NewGuid();

    var game = await gameService.StartGameAsync(playerId, Color.Black, 9, BotStrength.Superhuman);

    Assert.Equal(playerId, game.PlayerId);
    Assert.Equal(Color.Black, game.PlayerColor);
    Assert.Equal(9, game.BoardSize);
    Assert.Null(game.Outcome);
    Assert.Equal(Color.Black, game.Turn);

    var persisted = await repository.GetByIdAsync(game.Id);
    Assert.NotNull(persisted);
    Assert.Equal(game.Id, persisted.Id);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task StartGameAsync_NonPositiveBoardSize_Throws(int boardSize)
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    Guid playerId = Guid.NewGuid();

    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => gameService.StartGameAsync(playerId, Color.Black, boardSize, BotStrength.Superhuman));
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

  [Fact]
  public async Task MakeMoveAsync_LegalMove_SucceedsAndPersists()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    var result = await gameService.MakeMoveAsync(game.Id, Color.Black, 0, 0);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Single(result.Game.Moves);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(1, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakeMoveAsync_NonExistentGame_ReturnsNotFound()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    var result = await gameService.MakeMoveAsync(Guid.NewGuid(), Color.Black, 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakeMoveAsync_InvalidAction_FailsWithoutMutatingOrPersisting()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    // Invalid action using the wrong turn
    var result = await gameService.MakeMoveAsync(game.Id, Color.White, 0, 0);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_LegalPass_SucceedsAndPersists()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    var result = await gameService.MakePassAsync(game.Id, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Single(result.Game.Moves);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Null(result.Game.Outcome);
    Assert.Equal(1, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_NonExistentGame_ReturnsNotFound()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    var result = await gameService.MakePassAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_InvalidAction_FailsWithoutMutatingOrPersisting()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    // Invalid action using the wrong turn
    var result = await gameService.MakePassAsync(game.Id, Color.White);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_MoveToUndo_SucceedsAndPersists()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);
    Assert.True((await gameService.MakeMoveAsync(game.Id, Color.Black, 0, 0)).Success);

    var result = await gameService.UndoAsync(game.Id);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(2, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_NonExistentGame_ReturnsNotFound()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    var result = await gameService.UndoAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_InvalidAction_FailsWithoutMutatingOrPersisting()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    // Invalid action with nothing to undo
    var result = await gameService.UndoAsync(game.Id);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_ValidInput_SucceedsAndPersists()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    var game = await gameService.StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    var result = await gameService.ResignAsync(game.Id, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Equal(1, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_NonExistentGame_ReturnsNotFound()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);

    var result = await gameService.ResignAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_InvalidAction_FailsWithoutMutatingOrPersisting()
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    Guid gameId = Guid.NewGuid();
    Guid playerId = Guid.NewGuid();
    Game game = new(gameId, playerId, Color.Black, 9, Outcome.PlayerResigned);
    game.BuildPosition();
    Game expected = new(gameId, playerId, Color.Black, 9, Outcome.PlayerResigned);
    expected.BuildPosition();
    await repository.AddAsync(game);

    // Invalid action - game already finished
    var result = await gameService.ResignAsync(gameId, Color.Black);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equivalent(expected, result.Game);
    Assert.Equal(0, repository.SaveAsyncCallCount);
  }
}
