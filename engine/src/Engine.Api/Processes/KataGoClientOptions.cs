namespace Engine.Api.Processes;

/// <summary>
/// The grace period <see cref="KataGoClient"/> waits for queued/in-flight work to finish
/// naturally during shutdown, before cancelling it.
/// </summary>
public sealed class KataGoClientOptions
{
  /// <summary>
  /// Gets or sets the client shutdown grace period (in milliseconds) that determines how long
  /// <see cref="KataGoClient.DisposeAsync"/> waits for queued/in-flight work to finish naturally
  /// before cancelling it.
  /// </summary>
  public int ClientShutdownGracePeriodMs { get; set; } = 5000;
}
