using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Provides a suggested move and win-rate estimate for a game position, and a way to warm the
/// engine up ahead of time. Implemented by Infrastructure.
/// </summary>
public interface IEngineClient
{
  /// <summary>
  /// Gets a suggested move and win-rate estimate for the position resulting from replaying
  /// <paramref name="moveHistory"/> in order.
  /// </summary>
  /// <param name="moveHistory">The full move history to replay, from the start of the game.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="komi">The game's komi.</param>
  /// <param name="strength">The strength to answer at.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The suggested move and win-rate estimate.</returns>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something
  /// that cannot be read.
  /// </exception>
  Task<EngineSuggestion> GetSuggestionAsync(
    IReadOnlyList<Move> moveHistory,
    int boardSize,
    double komi,
    BotStrength strength,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Asks the engine to load ahead of time, so that a later call to
  /// <see cref="GetSuggestionAsync"/> does not pay the load in front of a waiting user.
  /// </summary>
  /// <remarks>
  /// Safe to call repeatedly: an engine that is already warm returns immediately. The engine
  /// may take a long time to answer the first call, since it only returns once loading has
  /// finished — the caller's <paramref name="cancellationToken"/> is how to bound that wait
  /// more tightly than the underlying transport's own timeout.
  /// </remarks>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task that completes once the engine reports itself warm.</returns>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, does not answer in time, or rejects the request.
  /// </exception>
  Task WarmUpAsync(CancellationToken cancellationToken = default);
}
