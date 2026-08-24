using GoTrainingPlatform.Application.Orchestration;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// The engine's suggested move for the human player's next decision, and its win-rate estimate.
/// </summary>
/// <param name="Coordinates">
/// The suggested point, or <c>null</c> when the engine suggests passing.
/// </param>
/// <param name="BlackWinRate">The estimated probability that Black wins, from 0 to 1.</param>
public sealed record SuggestionResponse(CoordinatesResponse? Coordinates, double BlackWinRate)
{
  /// <summary>
  /// Builds a <see cref="SuggestionResponse"/> from an engine suggestion.
  /// </summary>
  /// <param name="suggestion">The suggestion to convert.</param>
  /// <returns>The response representation of <paramref name="suggestion"/>.</returns>
  public static SuggestionResponse From(EngineSuggestion suggestion) => new(
    suggestion.Coordinates is null
      ? null
      : new CoordinatesResponse(suggestion.Coordinates.X, suggestion.Coordinates.Y),
    suggestion.BlackWinRate);
}
