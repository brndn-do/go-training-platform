using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// Exercises <see cref="EngineClient"/> against a real running engine, proving the request it
/// actually serializes is accepted and its response is understood. Needs the engine up
/// (<c>docker compose up -d engine</c>) and <c>Engine__BaseUrl</c> exported.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EngineClientIntegrationTests
{
  private const int BoardSize = 9;

  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

  [Fact]
  public async Task GetSuggestionAsync_EmptyBoardAtSuperhuman_ReturnsSuggestionOnBoard()
  {
    EngineSuggestion suggestion = await CreateClient()
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman)
      .WaitAsync(_timeout);

    Assert.NotNull(suggestion.Coordinates);
    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_MoveHistoryWithPass_IsAcceptedByEngine()
  {
    // Black (2,2), White passes, Black (6,6).
    IReadOnlyList<Move> moveHistory =
      [new(new Coordinates(2, 2), 0), new(null, 1), new(new Coordinates(6, 6), 2)];

    EngineSuggestion suggestion = await CreateClient()
      .GetSuggestionAsync(moveHistory, BoardSize, 7.5, BotStrength.Superhuman)
      .WaitAsync(_timeout);

    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_RankedStrength_ReturnsSuggestion()
  {
    EngineSuggestion suggestion = await CreateClient()
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Kyu5)
      .WaitAsync(_timeout);

    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_InvalidKomi_ThrowsInvalidRequest()
  {
    // The engine rejects a komi that is neither an integer nor a half-integer.
    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient()
        .GetSuggestionAsync([], BoardSize, 7.3, BotStrength.Superhuman)
        .WaitAsync(_timeout));

    Assert.Equal(EngineFailureKind.InvalidRequest, exception.Kind);
  }

  private static void AssertPlausible(EngineSuggestion suggestion)
  {
    Assert.InRange(suggestion.BlackWinRate, 0.0, 1.0);

    // A renamed or missing blackWinRate field deserializes to 0.0, which is still "in range".
    Assert.NotEqual(0.0, suggestion.BlackWinRate);

    if (suggestion.Coordinates is not null)
    {
      Assert.InRange(suggestion.Coordinates.X, 0, BoardSize - 1);
      Assert.InRange(suggestion.Coordinates.Y, 0, BoardSize - 1);
    }
  }

  private static EngineClient CreateClient()
  {
    string baseUrl = Environment.GetEnvironmentVariable("Engine__BaseUrl")
      ?? throw new InvalidOperationException("Engine__BaseUrl is not set.");

    return new(new HttpClient { BaseAddress = new Uri(baseUrl) });
  }
}
