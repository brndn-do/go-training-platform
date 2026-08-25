using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// Exercises <see cref="EngineClient"/> against a real running engine, proving the request it
/// actually serializes is accepted and its response is understood. Needs the engine up
/// (<c>docker compose up -d engine</c>) and <c>Engine__BaseUrl</c> set in the repo-root <c>.env</c>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Engine")]
public sealed class EngineClientIntegrationTests
{
  private const int BoardSize = 9;
  private const string ReadyPath = "health/ready";

  // have all tasks time out so tests don't hang, chain tasks with .WaitAsync(_timeout)
  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
  private static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(10);

  [Fact]
  public async Task GetSuggestionAsync_EmptyBoardAtSuperhuman_ReturnsSuggestionOnBoard()
  {
    EngineClient engineClient = await CreateClientAsync();

    EngineSuggestion suggestion = await engineClient
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman)
      .WaitAsync(_timeout);

    Assert.NotNull(suggestion.Coordinates);
    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_MoveHistoryWithPass_IsAcceptedByEngine()
  {
    EngineClient engineClient = await CreateClientAsync();

    // Black (2,2), White passes, Black (6,6).
    IReadOnlyList<Move> moveHistory =
      [new(new Coordinates(2, 2), 0), new(null, 1), new(new Coordinates(6, 6), 2)];

    EngineSuggestion suggestion = await engineClient
      .GetSuggestionAsync(moveHistory, BoardSize, 7.5, BotStrength.Superhuman)
      .WaitAsync(_timeout);

    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_RankedStrength_ReturnsSuggestion()
  {
    EngineClient engineClient = await CreateClientAsync();

    EngineSuggestion suggestion = await engineClient
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Kyu5)
      .WaitAsync(_timeout);

    AssertPlausible(suggestion);
  }

  [Fact]
  public async Task GetSuggestionAsync_InvalidKomi_ThrowsInvalidRequest()
  {
    EngineClient engineClient = await CreateClientAsync();

    // The engine rejects a komi that is neither an integer nor a half-integer.
    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      engineClient
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

  private static async Task<EngineClient> CreateClientAsync()
  {
    string baseUrl = Environment.GetEnvironmentVariable("Engine__BaseUrl")
      ?? throw new InvalidOperationException("Engine__BaseUrl is not set.");

    var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

    await EnsureEngineReadyAsync(httpClient, baseUrl);

    return new(httpClient);
  }

  private static async Task EnsureEngineReadyAsync(HttpClient httpClient, string baseUrl)
  {
    using HttpResponseMessage response = await ProbeReadyAsync(httpClient, baseUrl);

    if (response.IsSuccessStatusCode)
    {
      return;
    }

    throw new InvalidOperationException(
      $"The engine at {baseUrl} answered {ReadyPath} with {(int)response.StatusCode}, so it is " +
      "running but not ready to serve queries. KataGo may still be loading its models — wait for " +
      "`docker ps` to show the engine as \"(healthy)\".");
  }

  private static async Task<HttpResponseMessage> ProbeReadyAsync(HttpClient httpClient, string baseUrl)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, ReadyPath);
    using var probeTimeout = new CancellationTokenSource(_probeTimeout);

    try
    {
      return await httpClient.SendAsync(request, probeTimeout.Token);
    }
    catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
    {
      throw new InvalidOperationException(
        $"The engine at {baseUrl} did not respond at {ReadyPath}. Is the container running? " +
        "Start it with `docker compose up -d engine`, then wait for `docker ps` to show it " +
        "as \"(healthy)\".",
        exception);
    }
  }
}
