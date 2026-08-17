using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Provides a suggested move and win-rate estimate for a game position, implemented by
/// Infrastructure.
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
}
