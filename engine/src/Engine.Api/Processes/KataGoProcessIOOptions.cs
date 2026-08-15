namespace Engine.Api.Processes;

/// <summary>
/// The shutdown grace period value for <see cref="KataGoProcessIO"/>.
/// </summary>
public sealed class KataGoProcessIOOptions
{
  /// <summary>
  /// Gets or sets the process shutdown grace period (in milliseconds) that determines
  /// how long to wait for the KataGo binary to shut down before force-killing.
  /// </summary>
  public int ProcessShutdownGracePeriodMs { get; set; } = 5000;
}
