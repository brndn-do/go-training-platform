namespace Engine.Api.Processes;

/// <summary>
/// The liveness threshold and shutdown grace period values for <see cref="KataGoClient"/>.
/// </summary>
public sealed class KataGoClientOptions
{
  /// <summary>
  /// Gets or sets the client liveness threshold (in milliseconds) that determines how long it should
  /// take for single query to get a response before the underlying KataGo process is considered stuck.
  /// </summary>
  public int ClientLivenessThresholdMs { get; set; } = 10000;

  /// <summary>
  /// Gets or sets the client shutdown grace period (in milliseconds) that determines how long
  /// <see cref="KataGoClient.DisposeAsync"/> waits for queued/in-flight work to finish naturally
  /// before cancelling it.
  /// </summary>
  public int ClientShutdownGracePeriodMs { get; set; } = 5000;
}
