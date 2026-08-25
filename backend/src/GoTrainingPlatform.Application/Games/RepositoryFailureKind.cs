namespace GoTrainingPlatform.Application.Games;

/// <summary>
/// The category of failure reported by a <see cref="RepositoryException"/>.
/// </summary>
public enum RepositoryFailureKind
{
  /// <summary>
  /// The store could not be reached, or did not answer in time. Transient: the same
  /// operation may succeed if attempted again.
  /// </summary>
  Unavailable,

  /// <summary>
  /// The game was changed by someone else since it was loaded, so the write was refused.
  /// The caller's copy is stale and must be reloaded rather than retried — an earlier part
  /// of the same request may already have been persisted.
  /// </summary>
  Conflict,

  /// <summary>
  /// The store rejected the operation for a reason that will not resolve on its own, such
  /// as a violated constraint.
  /// </summary>
  Rejected,
}
