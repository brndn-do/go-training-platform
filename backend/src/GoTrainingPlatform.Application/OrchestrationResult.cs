using GoTrainingPlatform.Domain;

namespace GoTrainingPlatform.Application;

/// <summary>
/// The result of a <see cref="TurnOrchestrator"/> operation.
/// </summary>
/// <param name="Game">
/// The affected game, or <c>null</c> only when no game with the given id exists.
/// </param>
/// <param name="Success">
/// <c>true</c> if the operation (and any human action it involved) was accepted; <c>false</c>
/// if a human action was rejected for any reason.
/// </param>
/// <param name="Suggestion">
/// The engine's suggested move and win-rate estimate for the human's next decision, or
/// <c>null</c> if none is available.
/// </param>
public sealed record OrchestrationResult(Game? Game, bool Success, EngineSuggestion? Suggestion);
