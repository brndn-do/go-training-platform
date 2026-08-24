namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// Settings for reaching the engine service.
/// </summary>
public sealed class EngineOptions
{
  /// <summary>
  /// The configuration section these options bind from.
  /// </summary>
  public const string SectionName = "Engine";

  /// <summary>
  /// Gets or sets the engine's base URL.
  /// </summary>
  public string BaseUrl { get; set; } = string.Empty;
}
