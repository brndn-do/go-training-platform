namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// The category of failure reported by an <see cref="EngineException"/>.
/// </summary>
public enum EngineFailureKind
{
  /// <summary>The engine could not be reached, or did not answer in time.</summary>
  Unavailable,

  /// <summary>The engine rejected the request as invalid.</summary>
  InvalidRequest,

  /// <summary>The engine answered, but its response could not be read.</summary>
  InvalidResponse,
}
