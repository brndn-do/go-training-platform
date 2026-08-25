namespace GoTrainingPlatform.Application.Games;

/// <summary>
/// Thrown when <see cref="IGameRepository"/> cannot complete an operation. Carries no type
/// from the underlying store, so callers never depend on how games are persisted.
/// </summary>
/// <param name="kind">The category of failure.</param>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying exception, if any.</param>
public sealed class RepositoryException(
  RepositoryFailureKind kind,
  string message,
  Exception? innerException = null)
  : Exception(message, innerException)
{
  /// <summary>
  /// Gets the category of failure.
  /// </summary>
  public RepositoryFailureKind Kind { get; } = kind;
}
