using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application;

/// <summary>
/// The engine's suggested next move for a position, from <see cref="IEngineClient.GetSuggestionAsync"/>.
/// </summary>
/// <param name="Coordinates">
/// The suggested move's coordinates, or <c>null</c> if the engine suggests passing.
/// </param>
/// <param name="BlackWinRate">
/// Black's estimated win probability (0 to 1) for the position, fixed to Black regardless of
/// whose turn it is or which color requested the suggestion.
/// </param>
public sealed record EngineSuggestion(Coordinates? Coordinates, double BlackWinRate);
