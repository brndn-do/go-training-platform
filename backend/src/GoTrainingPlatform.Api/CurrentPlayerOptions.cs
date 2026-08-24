namespace GoTrainingPlatform.Api;

/// <summary>
/// Identifies the player every request acts as, until real authentication exists.
/// </summary>
public sealed class CurrentPlayerOptions
{
  /// <summary>
  /// The configuration section these options bind from.
  /// </summary>
  public const string SectionName = "CurrentPlayer";

  /// <summary>
  /// Gets or sets the player's id.
  /// </summary>
  public Guid Id { get; set; }
}
