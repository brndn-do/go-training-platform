using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Application.Tests.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests.Orchestration;

public sealed class TurnOrchestratorTests
{
  [Fact]
  public async Task StartGameAsync_PlayerColorBlack_ReturnsGameAndHintWithoutBotPlay()
  {
    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);
    var result = await Orch([suggestion]).StartGameAsync(Guid.NewGuid(), Color.Black, 9, BotStrength.Superhuman);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestion, result.Suggestion);
  }

  [Fact]
  public async Task StartGameAsync_PlayerColorWhite_ReturnsGameAndHintWithBotMove()
  {
    EngineSuggestion suggestionForBot = new(new Coordinates(0, 0), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(1, 1), 0.5);

    var result = await Orch([suggestionForBot, suggestionForHuman]).
      StartGameAsync(Guid.NewGuid(), Color.White, 9, BotStrength.Superhuman);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(suggestionForBot.Coordinates, result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task StartGameAsync_PlayerColorWhite_ReturnsGameAndHintWithBotPass()
  {
    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(0, 0), 0.5);

    var result = await Orch([suggestionForBot, suggestionForHuman]).
      StartGameAsync(Guid.NewGuid(), Color.White, 9, BotStrength.Superhuman);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Null(result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orch([]).LoadGameAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_HumanTurn_ReturnsGameAndHintWithoutBotPlay()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);
    var result = await Orch([suggestion], repository).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestion, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_BotTurn_ReturnsGameAndHintWithBotMove()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.White, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(new Coordinates(0, 0), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(1, 1), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(suggestionForBot.Coordinates, result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_BotTurn_ReturnsGameAndHintWithBotPass()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.White, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(0, 0), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Null(result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orch([]).MakeMoveAsync(Guid.NewGuid(), Color.Black, 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_InvalidAction_ReturnsUnchangedGameWithoutSuggestion()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    // Invalid action using the wrong turn
    var result = await Orch([], repository).MakeMoveAsync(gameId, Color.White, 0, 0);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_ValidMove_ReturnsGameAndHintWithBotMove()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(new Coordinates(1, 1), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).MakeMoveAsync(gameId, Color.Black, 0, 0);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(2, result.Game.Moves.Count);
    Assert.Equal(new Coordinates(0, 0), result.Game.Moves[0].Coordinates);
    Assert.Equal(suggestionForBot.Coordinates, result.Game.Moves[1].Coordinates);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_ValidMove_ReturnsGameAndHintWithBotPass()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).MakeMoveAsync(gameId, Color.Black, 0, 0);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(2, result.Game.Moves.Count);
    Assert.Null(result.Game.Moves[1].Coordinates);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orch([]).MakePassAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_InvalidAction_ReturnsUnchangedGameWithoutSuggestion()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    // Invalid action using the wrong turn
    var result = await Orch([], repository).MakePassAsync(gameId, Color.White);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_ValidPass_ReturnsGameAndHintWithBotMove()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(new Coordinates(1, 1), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).MakePassAsync(gameId, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(2, result.Game.Moves.Count);
    Assert.Null(result.Game.Moves[0].Coordinates);
    Assert.Equal(suggestionForBot.Coordinates, result.Game.Moves[1].Coordinates);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_ValidPass_ReturnsGameAndHintWithBotPass()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);
    var result = await Orch([suggestionForBot, suggestionForHuman], repository).MakePassAsync(gameId, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(2, result.Game.Moves.Count);
    Assert.Null(result.Game.Moves[1].Coordinates);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
    Assert.Equal(Outcome.TwoConsecutivePasses, result.Game.Outcome);
  }

  [Fact]
  public async Task UndoAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orch([]).UndoAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task UndoAsync_InvalidAction_ReturnsUnchangedGameWithoutSuggestion()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    // Invalid action with nothing to undo
    var result = await Orch([], repository).UndoAsync(gameId);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task UndoAsync_ValidUndo_ReturnsGameAndHint()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    List<Move> moveHistory = [new Move(new Coordinates(0, 0), 0), new Move(new Coordinates(1, 1), 1)];
    Game game = new(moveHistory, gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestion = new(new Coordinates(2, 2), 0.5);
    var result = await Orch([suggestion], repository).UndoAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestion, result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orch([]).ResignAsync(Guid.NewGuid(), Color.Black);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_InvalidAction_ReturnsUnchangedGameWithoutSuggestion()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, Outcome.PlayerResigned);
    await repository.AddAsync(game);

    // Invalid action - game already finished
    var result = await Orch([], repository).ResignAsync(gameId, Color.Black);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_ValidResign_ReturnsFinishedGameWithHint()
  {
    FakeGameRepository repository = new();
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    await repository.AddAsync(game);

    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);
    var result = await Orch([suggestion], repository).ResignAsync(gameId, Color.Black);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Equal(suggestion, result.Suggestion);
  }

  // given a list of engine suggestions, returns a fresh orchestrator that will return
  // each suggestion in its responses in order, one per call. Uses the given repository
  // if provided, so a test can seed a pre-existing game before loading it.
  private static TurnOrchestrator Orch(IReadOnlyList<EngineSuggestion> suggestions, FakeGameRepository? repository = null)
  {
    GameService gameService = new(repository ?? new());
    FakeEngineClient engineClient = new(suggestions);
    return new(gameService, engineClient);
  }
}
