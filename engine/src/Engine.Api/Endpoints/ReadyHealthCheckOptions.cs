namespace Engine.Api.Endpoints;

/// <summary>
/// The process-unresponsive threshold value for <see cref="ReadyHealthCheck"/>.
/// </summary>
public sealed class ReadyHealthCheckOptions
{
  /// <summary>
  /// Gets or sets how long (in milliseconds) a single query may stay in flight before
  /// <see cref="ReadyHealthCheck"/> considers the process unresponsive.
  /// </summary>
  public int ProcessUnresponsiveThresholdMs { get; set; } = 10000;
}
