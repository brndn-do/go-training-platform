namespace Engine.Api.Processes;

/// <summary>
/// Locations of the KataGo binary, its analysis-engine config file, and the two models it
/// loads. Bound from the "KataGo" configuration section.
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
}
