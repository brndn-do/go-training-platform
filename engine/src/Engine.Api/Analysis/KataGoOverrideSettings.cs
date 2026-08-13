namespace Engine.Api.Analysis;

/// <summary>
/// Override settings for KataGo, including HumanSLProfile, used for the HumanSL model.
/// </summary>
public sealed record KataGoOverrideSettings
{
  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoOverrideSettings"/> class.
  /// </summary>
  /// <param name="botStrength">The bot strength. Must not be Superhuman.</param>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="botStrength"/> is Superhuman.
  /// </exception>
  public KataGoOverrideSettings(BotStrength botStrength)
  {
    if (botStrength.IsSuperhuman)
    {
      throw new ArgumentException("KataGoOverrideSettings cannot be constructed for Superhuman strength.", nameof(botStrength));
    }

    HumanSLProfile = MapBotStrength(botStrength.Value);
  }

  /// <summary>
  /// Gets the HumanSLProfile to pass.
  /// </summary>
  public string HumanSLProfile { get; }

  private static string MapBotStrength(string botStrength)
  {
    int num = int.Parse(botStrength.AsSpan(3));
    char system = botStrength[..3] == "Kyu" ? 'k' : 'd';

    return $"rank_{num}{system}";
  }
}