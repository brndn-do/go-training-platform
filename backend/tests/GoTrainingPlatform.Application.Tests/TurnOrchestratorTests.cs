using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests;

public class TurnOrchestratorTests
{
  [Fact]
  public async Task StartGameAsync_PlayerColorBlack_ReturnsGameAndHintWithoutBotMove()
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

  // given a list of engine suggestions, returns a fresh orchestrator that will return
  // each suggestion in its responses in order, one per call.
  private static TurnOrchestrator Orch(IReadOnlyList<EngineSuggestion> suggestions)
  {
    FakeGameRepository repository = new();
    GameService gameService = new(repository);
    FakeEngineClient engineClient = new(suggestions);
    return new(gameService, engineClient);
  }
}
