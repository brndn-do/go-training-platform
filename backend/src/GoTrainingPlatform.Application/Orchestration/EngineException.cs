namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Thrown when <see cref="IEngineClient"/> cannot produce a suggestion.
/// </summary>
/// <param name="kind">The category of failure.</param>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying exception, if any.</param>
public sealed class EngineException(
  EngineFailureKind kind,
  string message,
  Exception? innerException = null)
  : Exception(message, innerException)
{
  /// <summary>
  /// Gets the category of failure.
  /// </summary>
  public EngineFailureKind Kind { get; } = kind;
}
