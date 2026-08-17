using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// HTTP implementation of <see cref="IEngineClient"/>, calling the engine's
/// <c>POST /suggestion</c> endpoint.
/// </summary>
public sealed class EngineClient(HttpClient httpClient) : IEngineClient
{
  private const string RequestUri = "/suggestion";

  /// <inheritdoc/>
  public async Task<EngineSuggestion> GetSuggestionAsync(
    IReadOnlyList<Move> moveHistory,
    int boardSize,
    double komi,
    BotStrength strength,
    CancellationToken cancellationToken = default)
  {
    var suggestionRequest = ToSuggestionRequest(moveHistory, boardSize, komi, strength);

    try
    {
      using var result = await httpClient.PostAsJsonAsync(
        RequestUri, suggestionRequest, cancellationToken);

      if (result.StatusCode != HttpStatusCode.OK)
      {
        throw new EngineException(
          (int)result.StatusCode >= 500
            ? EngineFailureKind.Unavailable
            : EngineFailureKind.InvalidRequest,
          $"The engine responded with {(int)result.StatusCode}.");
      }

      Engine.SuggestionResponse response = await result
        .Content.ReadFromJsonAsync<Engine.SuggestionResponse>(cancellationToken)
        ?? throw new EngineException(
          EngineFailureKind.InvalidResponse, "The engine's response was empty.");

      return ToEngineSuggestion(response);
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
