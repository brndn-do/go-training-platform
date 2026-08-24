using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Application.Tests.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests.Orchestration;

public sealed class TurnOrchestratorTests
{
  private readonly FakeGameRepository _repository = new();
  private readonly Guid _playerId = Guid.NewGuid();

  [Fact]
  public async Task StartGameAsync_PlayerColorBlack_ReturnsGameAndHintWithoutBotPlay()
  {
    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);

    var result = await Orchestrator([suggestion]).StartGameAsync(Color.Black, 9, BotStrength.Superhuman);

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

    var result = await Orchestrator([suggestionForBot, suggestionForHuman])
      .StartGameAsync(Color.White, 9, BotStrength.Superhuman);

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

    var result = await Orchestrator([suggestionForBot, suggestionForHuman])
      .StartGameAsync(Color.White, 9, BotStrength.Superhuman);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Null(result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orchestrator([]).LoadGameAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_HumanTurn_ReturnsGameAndHintWithoutBotPlay()
  {
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);

    var result = await Orchestrator([suggestion]).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestion, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_BotTurn_ReturnsGameAndHintWithBotMove()
  {
    Guid gameId = await SeedGameAsync(playerColor: Color.White);
    EngineSuggestion suggestionForBot = new(new Coordinates(0, 0), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(1, 1), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(suggestionForBot.Coordinates, result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task LoadGameAsync_BotTurn_ReturnsGameAndHintWithBotPass()
  {
    Guid gameId = await SeedGameAsync(playerColor: Color.White);
    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(0, 0), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).LoadGameAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Null(result.Game.Moves[0].Coordinates);
    Assert.Equal(Color.White, result.Game.Turn);
    Assert.Equal(suggestionForHuman, result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orchestrator([]).MakeMoveAsync(Guid.NewGuid(), 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_CallerDoesNotOwnGame_ReturnsNotFoundWithoutCallingEngine()
  {
    Guid gameId = await SeedGameAsync();

    // an otherwise-legal move, so ownership is the only reason it can be rejected. No
    // suggestions are supplied, so reaching the engine at all would throw.
    var result = await Orchestrator([], playerId: Guid.NewGuid()).MakeMoveAsync(gameId, 0, 0);

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_WrongTurn_ReturnsUnchangedGameWithoutSuggestion()
  {
    // the human plays White, so it is Black's — the bot's — turn first
    Guid gameId = await SeedGameAsync(playerColor: Color.White);

    var result = await Orchestrator([]).MakeMoveAsync(gameId, 0, 0);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakeMoveAsync_ValidMove_ReturnsGameAndHintWithBotMove()
  {
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestionForBot = new(new Coordinates(1, 1), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).MakeMoveAsync(gameId, 0, 0);

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
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).MakeMoveAsync(gameId, 0, 0);

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
    var result = await Orchestrator([]).MakePassAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_WrongTurn_ReturnsUnchangedGameWithoutSuggestion()
  {
    // the human plays White, so it is Black's — the bot's — turn first
    Guid gameId = await SeedGameAsync(playerColor: Color.White);

    var result = await Orchestrator([]).MakePassAsync(gameId);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task MakePassAsync_ValidPass_ReturnsGameAndHintWithBotMove()
  {
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestionForBot = new(new Coordinates(1, 1), 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).MakePassAsync(gameId);

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
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestionForBot = new(null, 0.5);
    EngineSuggestion suggestionForHuman = new(new Coordinates(2, 2), 0.5);

    var result = await Orchestrator([suggestionForBot, suggestionForHuman]).MakePassAsync(gameId);

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
    var result = await Orchestrator([]).UndoAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task UndoAsync_NothingToUndo_ReturnsUnchangedGameWithoutSuggestion()
  {
    Guid gameId = await SeedGameAsync();

    var result = await Orchestrator([]).UndoAsync(gameId);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task UndoAsync_ValidUndo_ReturnsGameAndHint()
  {
    List<Move> moveHistory = [new Move(new Coordinates(0, 0), 0), new Move(new Coordinates(1, 1), 1)];
    Guid gameId = await SeedGameAsync(moveHistory: moveHistory);
    EngineSuggestion suggestion = new(new Coordinates(2, 2), 0.5);

    var result = await Orchestrator([suggestion]).UndoAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Empty(result.Game.Moves);
    Assert.Equal(Color.Black, result.Game.Turn);
    Assert.Equal(suggestion, result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_NonExistentGame_ReturnsNotFound()
  {
    var result = await Orchestrator([]).ResignAsync(Guid.NewGuid());

    Assert.False(result.Success);
    Assert.Null(result.Game);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_GameAlreadyFinished_ReturnsUnchangedGameWithoutSuggestion()
  {
    Guid gameId = await SeedGameAsync(outcome: Outcome.PlayerResigned);

    var result = await Orchestrator([]).ResignAsync(gameId);

    Assert.False(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Null(result.Suggestion);
  }

  [Fact]
  public async Task ResignAsync_ValidResign_ReturnsFinishedGameWithHint()
  {
    Guid gameId = await SeedGameAsync();
    EngineSuggestion suggestion = new(new Coordinates(0, 0), 0.5);

    var result = await Orchestrator([suggestion]).ResignAsync(gameId);

    Assert.True(result.Success);
    Assert.NotNull(result.Game);
    Assert.Equal(Outcome.PlayerResigned, result.Game.Outcome);
    Assert.Equal(suggestion, result.Suggestion);
  }

  // adds a 9x9 game owned by the current player to the repository, and returns its id.
  private async Task<Guid> SeedGameAsync(
    Color playerColor = Color.Black,
    Outcome? outcome = null,
    IReadOnlyList<Move>? moveHistory = null)
  {
    Guid gameId = Guid.NewGuid();
    Game game = moveHistory is null
      ? new(gameId, _playerId, playerColor, 9, outcome)
      : new(moveHistory, gameId, _playerId, playerColor, 9, outcome);

    await _repository.AddAsync(game);
    return gameId;
  }

  // an orchestrator over the shared repository, returning each suggestion in order, one per
  // call — supplying fewer than the test expects makes an unexpected engine call throw.
  // Acts as the player who owns seeded games unless a different id is given.
  private TurnOrchestrator Orchestrator(IReadOnlyList<EngineSuggestion> suggestions, Guid? playerId = null)
  {
    FakeCurrentPlayer player = new(playerId ?? _playerId);
    GameService gameService = new(player, _repository);
    FakeEngineClient engineClient = new(suggestions);
    return new(gameService, engineClient);
  }
}
