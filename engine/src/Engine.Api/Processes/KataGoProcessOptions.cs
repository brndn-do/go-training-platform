namespace Engine.Api.Processes;

/// <summary>
/// Locations of the KataGo binary, its analysis-engine config file, the two models it
/// loads, and the grace period of the process shutdown.
/// </summary>
public sealed class KataGoProcessOptions
{
  /// <summary>
  /// Gets or sets the path to the KataGo executable.
  /// </summary>
  public string BinaryPath { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the path to the KataGo analysis-engine config file.
  /// </summary>
  public string ConfigPath { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the path to the strong self-play model, used for <c>Superhuman</c> strength
  /// and hints.
  /// </summary>
  public string ModelPath { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the path to the human SL model, used for ranked bot strengths.
  /// </summary>
  public string HumanModelPath { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the process shutdown grace period (in milliseconds) that determines
  /// how long to wait for the KataGo binary to shut down before force-killing.
  /// </summary>
  public int ProcessShutdownGracePeriodMs { get; set; } = 5000;
}
