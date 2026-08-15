namespace Engine.Api.Endpoints;

/// <summary>
/// The process-unresponsive threshold value for <see cref="LiveHealthCheck"/>.
/// </summary>
public sealed class LiveHealthCheckOptions
{
  /// <summary>
  /// Gets or sets how long (in milliseconds) a single query may stay in flight before
  /// <see cref="LiveHealthCheck"/> considers the process unresponsive.
  /// </summary>
  public int ProcessUnresponsiveThresholdMs { get; set; } = 30000;
}
