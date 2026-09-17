namespace GoTrainingPlatform.Api;

/// <summary>
/// Settings for which origins may call the API with credentials.
/// </summary>
public sealed class CorsOptions
{
  /// <summary>
  /// The configuration section these options bind from.
  /// </summary>
  public const string SectionName = "Cors";

  /// <summary>
  /// Gets or sets the origins allowed to call the API with credentials (the SPA, per
  /// environment). Bind as <c>Cors__AllowedOrigins__0</c>, <c>Cors__AllowedOrigins__1</c>, etc.
  /// </summary>
  public string[] AllowedOrigins { get; set; } = [];
}
