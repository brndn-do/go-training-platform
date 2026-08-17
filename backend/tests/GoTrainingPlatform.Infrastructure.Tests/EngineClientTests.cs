using System.Net;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Infrastructure.Tests;

public sealed class EngineClientTests
{
  private const int BoardSize = 9;

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsMove_MapsToEngineSuggestion()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, """{"move":{"x":3,"y":4},"blackWinRate":0.62}""");

    EngineSuggestion suggestion = await CreateClient(handler)
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman);

    Assert.Equal(new Coordinates(3, 4), suggestion.Coordinates);
    Assert.Equal(0.62, suggestion.BlackWinRate);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsPass_MapsToNullCoordinates()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, """{"move":null,"blackWinRate":0.5}""");

    EngineSuggestion suggestion = await CreateClient(handler)
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman);

    Assert.Null(suggestion.Coordinates);
  }

  [Fact]
  public async Task GetSuggestionAsync_MoveHistoryWithPass_SendsMovesInOrderWithNullForPass()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "{}");

    IReadOnlyList<Move> moveHistory = [
      new(new Coordinates(1, 1), 1),
      new(null, 2),
      new(new Coordinates(2, 2), 3)];

    await CreateClient(handler)
      .GetSuggestionAsync(moveHistory, BoardSize, 7.5, BotStrength.Superhuman);

    Assert.Contains(
      """
      "moves":[{"x":1,"y":1},null,{"x":2,"y":2}]
      """,
      handler.LastRequestBody);
  }

  [Fact]
  public async Task GetSuggestionAsync_AnyStrength_SendsStrengthAsEngineString()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "{}");

    await CreateClient(handler)
      .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Kyu20);

    Assert.Contains(
      """
      "botStrength":"Kyu20"
      """,
      handler.LastRequestBody);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsBadRequest_ThrowsInvalidRequest()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.BadRequest, "{}");

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.InvalidRequest, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsServerError_ThrowsUnavailable()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.InternalServerError, "{}");

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.Unavailable, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsMalformedJson_ThrowsInvalidResponse()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "{");

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.InvalidResponse, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineReturnsNullBody_ThrowsInvalidResponse()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "null");

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.InvalidResponse, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_TransportFails_ThrowsUnavailable()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "null", exceptionToThrow: new HttpRequestException());

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.Unavailable, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_ResponseBodyCutShort_ThrowsUnavailable()
  {
    FakeHttpMessageHandler handler = new(HttpStatusCode.OK, new UnreadableHttpContent());

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      CreateClient(handler)
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    // HttpContent wraps a failed body read in HttpRequestException, so it reads as transport failure.
    Assert.Equal(EngineFailureKind.Unavailable, exception.Kind);
    Assert.IsType<HttpRequestException>(exception.InnerException);
  }

  [Fact]
  public async Task GetSuggestionAsync_EngineDoesNotAnswerBeforeTimeout_ThrowsUnavailable()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "{}", delay: TimeSpan.FromSeconds(30));

    HttpClient httpClient = new(handler)
    {
      BaseAddress = new Uri("http://engine"),
      Timeout = TimeSpan.FromMilliseconds(100),
    };

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      new EngineClient(httpClient).GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.Unavailable, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_ClientHasNoBaseAddress_ThrowsInvalidRequest()
  {
    FakeHttpMessageHandler handler = new(HttpStatusCode.OK, "{}");

    var exception = await Assert.ThrowsAsync<EngineException>(() =>
      new EngineClient(new HttpClient(handler))
        .GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Superhuman));

    Assert.Equal(EngineFailureKind.InvalidRequest, exception.Kind);
  }

  [Fact]
  public async Task GetSuggestionAsync_CallerCancels_PropagatesCancellation()
  {
    FakeHttpMessageHandler handler = new(
      HttpStatusCode.OK, "{}");

    CancellationTokenSource cts = new();

    await cts.CancelAsync();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      CreateClient(handler).GetSuggestionAsync([], BoardSize, 7.5, BotStrength.Kyu20, cts.Token));
  }

  private static EngineClient CreateClient(FakeHttpMessageHandler handler) =>
    new(new HttpClient(handler) { BaseAddress = new Uri("http://engine") });
}
