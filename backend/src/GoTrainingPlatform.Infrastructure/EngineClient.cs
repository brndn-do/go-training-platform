using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// HTTP implementation of <see cref="IEngineClient"/>, calling the engine's
/// <c>POST /suggestion</c> and <c>POST /warmup</c> endpoints.
/// </summary>
public sealed class EngineClient(HttpClient httpClient) : IEngineClient
{
  private const string SuggestionUri = "/suggestion";
  private const string WarmUpUri = "/warmup";

  /// <inheritdoc/>
  public async Task<EngineSuggestion> GetSuggestionAsync(
    IReadOnlyList<Move> moveHistory,
    int boardSize,
    double komi,
    BotStrength strength,
    CancellationToken cancellationToken = default)
  {
    var suggestionRequest = ToSuggestionRequest(moveHistory, boardSize, komi, strength);

    return await TranslateFailuresAsync(
      async () =>
      {
        using var result = await httpClient.PostAsJsonAsync(
          SuggestionUri, suggestionRequest, cancellationToken);

        EnsureSuccess(result);

        Engine.SuggestionResponse response = await result
          .Content.ReadFromJsonAsync<Engine.SuggestionResponse>(cancellationToken)
          ?? throw new EngineException(
            EngineFailureKind.InvalidResponse, "The engine's response was empty.");

        return ToEngineSuggestion(response);
      },
      cancellationToken);
  }

  /// <inheritdoc/>
  public async Task WarmUpAsync(CancellationToken cancellationToken = default)
  {
    await TranslateFailuresAsync<object?>(
      async () =>
      {
        using var request = new HttpRequestMessage(HttpMethod.Post, WarmUpUri);

        // Warm-up carries no request or response body — the status code is the whole contract,
        // so stop at the headers rather than buffering a body a truncated read could fail on.
        using HttpResponseMessage result = await httpClient.SendAsync(
          request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        EnsureSuccess(result);

        return null;
      },
      cancellationToken);
  }

  /// <summary>
  /// Throws if the engine answered with anything other than <see cref="HttpStatusCode.OK"/>,
  /// splitting a rejected request from an engine that could not serve it.
  /// </summary>
  /// <param name="result">The engine's response.</param>
  /// <exception cref="EngineException">If the status code is not 200.</exception>
  private static void EnsureSuccess(HttpResponseMessage result)
  {
    if (result.StatusCode == HttpStatusCode.OK)
    {
      return;
    }

    throw new EngineException(
      (int)result.StatusCode >= 500
        ? EngineFailureKind.Unavailable
        : EngineFailureKind.InvalidRequest,
      $"The engine responded with {(int)result.StatusCode}.");
  }

  /// <summary>
  /// Runs <paramref name="call"/>, turning every transport and serialization failure into an
  /// <see cref="EngineException"/> so no <see cref="HttpClient"/> type escapes this class. A
  /// cancellation the caller asked for propagates untouched — including one already requested
  /// before the call, which is refused without reaching the engine at all.
  /// </summary>
  /// <typeparam name="T">The call's result type.</typeparam>
  /// <param name="call">The call to make against the engine.</param>
  /// <param name="cancellationToken">The token the caller passed in.</param>
  /// <returns>The call's result.</returns>
  /// <exception cref="EngineException">If the call fails.</exception>
  private static async Task<T> TranslateFailuresAsync<T>(
    Func<Task<T>> call,
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    try
    {
      return await call();
    }
    catch (HttpRequestException ex)
    {
      throw new EngineException(
        EngineFailureKind.Unavailable, "The engine could not be reached.", ex);
    }
    catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
    {
      throw new EngineException(
        EngineFailureKind.Unavailable, "The engine did not respond in time.", ex);
    }
    catch (InvalidOperationException ex)
    {
      throw new EngineException(
        EngineFailureKind.InvalidRequest, "The engine request was misconfigured.", ex);
    }
    catch (JsonException ex)
    {
      throw new EngineException(
        EngineFailureKind.InvalidResponse, "The engine's response could not be read.", ex);
    }
  }

  private static Engine.SuggestionRequest ToSuggestionRequest(
    IReadOnlyList<Move> moveHistory,
    int boardSize,
    double komi,
    BotStrength strength)
  {
    IReadOnlyList<Engine.Move?> moves = [.. moveHistory.Select(m =>
      m.Coordinates is null ? null
      : new Engine.Move(m.Coordinates.X, m.Coordinates.Y))];

    string botStrength = strength.ToString();

    return new(moves, boardSize, komi, botStrength);
  }

  private static EngineSuggestion ToEngineSuggestion(Engine.SuggestionResponse response)
  {
    Coordinates? coordinates = response.Move is null ? null
      : new(response.Move.X, response.Move.Y);

    return new EngineSuggestion(coordinates, response.BlackWinRate);
  }
}
