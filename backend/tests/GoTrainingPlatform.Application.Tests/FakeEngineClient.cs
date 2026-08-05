using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Tests;

/// <summary>
/// Returns each <see cref="EngineSuggestion"/> in the list it was constructed with, one per
/// call, in order. Throws if called more times than there are suggestions — an unexpected
/// extra call likely signals a bug in the code under test, not something to silently tolerate.
/// Used to test <see cref="TurnOrchestrator"/>'s own logic in isolation.
/// </summary>
public class FakeEngineClient(IReadOnlyList<EngineSuggestion> suggestions) : IEngineClient
{
  public int CallCount { get; private set; }

  /// <inheritdoc/>
  public Task<EngineSuggestion> GetSuggestionAsync(
    IReadOnlyList<Move> moveHistory,
    int boardSize,
    double komi,
    BotStrength strength,
    CancellationToken cancellationToken = default)
  {
    // index out of bounds exception when called more than intended
    var result = Task.FromResult(suggestions[CallCount]);
    CallCount += 1;
    return result;
  }
}
