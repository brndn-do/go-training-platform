using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests.Games;

public sealed class GameServiceTests
{
  private readonly FakeGameRepository _repository = new();
  private readonly FakeCurrentPlayer _player = new(Guid.NewGuid());
  private readonly GameService _gameService;

  public GameServiceTests() => _gameService = new(_player, _repository);

  [Fact]
  public async Task StartGameAsync_ValidInput_CreatesAndPersistsGame()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    Assert.Equal(_player.Id, game.PlayerId);
    Assert.Equal(Color.Black, game.PlayerColor);
    Assert.Equal(9, game.BoardSize);
    Assert.Null(game.Outcome);
    Assert.Equal(Color.Black, game.Turn);

    var persisted = await _repository.GetByIdAsync(game.Id);
    Assert.NotNull(persisted);
    Assert.Equal(game.Id, persisted.Id);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task StartGameAsync_NonPositiveBoardSize_Throws(int boardSize)
  {
    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
      () => _gameService.StartGameAsync(Color.Black, boardSize, BotStrength.Superhuman));
  }

  [Fact]
  public async Task LoadGameAsync_ExistingGame_ReturnsGameWithPositionBuilt()
  {
    // seeded without calling BuildPosition: the fake repository hands back the same
    // reference it was given, so a pre-built position would hide whether LoadGameAsync
    // builds one itself.
    Guid gameId = Guid.NewGuid();
    Game game = new([new Move(new Coordinates(0, 0), 0)], gameId, _player.Id, Color.Black, 9, null);
    await _repository.AddAsync(game);

    var result = await _gameService.LoadGameAsync(gameId);

    Assert.NotNull(result);
    Assert.Equivalent(game, result);
    Assert.Equal(Color.White, result.Turn); // check position was built
  }

  [Fact]
  public async Task LoadGameAsync_NonExistentGame_ReturnsNull()
  {
    var result = await _gameService.LoadGameAsync(Guid.NewGuid());

    Assert.Null(result);
  }

  [Fact]
  public async Task LoadGameAsync_CallerDoesNotOwnGame_ReturnsNull()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await ServiceForOtherPlayer().LoadGameAsync(game.Id);

    Assert.Null(result);
  }

  [Fact]
  public async Task MakeMoveAsync_LegalMove_SucceedsAndPersists()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.MakeMoveAsync(game.Id, Color.Black, 0, 0);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Single(result.Game.Moves);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(1, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakeMoveAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await _gameService.MakeMoveAsync(Guid.NewGuid(), Color.Black, 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakeMoveAsync_WrongTurn_FailsWithoutMutatingOrPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.MakeMoveAsync(game.Id, Color.White, 0, 0);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakeMoveAsync_CallerDoesNotOwnGame_ReturnsNotFoundWithoutPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    // an otherwise-legal move, so ownership is the only reason it can be rejected
    var result = await ServiceForOtherPlayer().MakeMoveAsync(game.Id, Color.Black, 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_LegalPass_SucceedsAndPersists()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.MakePassAsync(game.Id, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Single(result.Game.Moves);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Null(result.Game.Outcome);
    Assert.Equal(1, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await _gameService.MakePassAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_WrongTurn_FailsWithoutMutatingOrPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.MakePassAsync(game.Id, Color.White);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task MakePassAsync_CallerDoesNotOwnGame_ReturnsNotFoundWithoutPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    // an otherwise-legal pass, so ownership is the only reason it can be rejected
    var result = await ServiceForOtherPlayer().MakePassAsync(game.Id, Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_MoveToUndo_SucceedsAndPersists()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);
    Assert.True((await _gameService.MakeMoveAsync(game.Id, Color.Black, 0, 0)).Success);

    var result = await _gameService.UndoAsync(game.Id);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(2, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await _gameService.UndoAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_NothingToUndo_FailsWithoutMutatingOrPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.UndoAsync(game.Id);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task UndoAsync_CallerDoesNotOwnGame_ReturnsNotFoundWithoutPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);
    Assert.True((await _gameService.MakeMoveAsync(game.Id, Color.Black, 0, 0)).Success);

    // there is a move to undo, so ownership is the only reason it can be rejected
    var result = await ServiceForOtherPlayer().UndoAsync(game.Id);

    Assert.False(result.Success);
    Assert.Null(result.Game);

    // the owner's move persisted once; the rejected undo must not persist again
    Assert.Equal(1, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_GameInProgress_SucceedsAndPersists()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    var result = await _gameService.ResignAsync(game.Id, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Equal(1, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await _gameService.ResignAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_GameAlreadyFinished_FailsWithoutMutatingOrPersisting()
  {
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, _player.Id, Color.Black, 9, Outcome.PlayerResigned);
    game.BuildPosition();
    await _repository.AddAsync(game);

    // an independent copy, since the fake repository hands back the same reference the
    // service mutates — comparing against `game` itself would pass either way.
    Game expected = new(gameId, _player.Id, Color.Black, 9, Outcome.PlayerResigned);
    expected.BuildPosition();

    var result = await _gameService.ResignAsync(gameId, Color.Black);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equivalent(expected, result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  [Fact]
  public async Task ResignAsync_CallerDoesNotOwnGame_ReturnsNotFoundWithoutPersisting()
  {
    var game = await _gameService.StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

    // the game is still in progress, so ownership is the only reason it can be rejected
    var result = await ServiceForOtherPlayer().ResignAsync(game.Id, Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Equal(0, _repository.SaveAsyncCallCount);
  }

  // a service acting as somebody other than the player who owns the seeded games,
  // over the same repository.
  private GameService ServiceForOtherPlayer() => new(new FakeCurrentPlayer(Guid.NewGuid()), _repository);
}
