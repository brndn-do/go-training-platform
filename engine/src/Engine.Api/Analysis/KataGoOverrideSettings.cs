using System.Text.RegularExpressions;

namespace Engine.Api.Analysis;

/// <summary>
/// Override settings for KataGo, including HumanSLProfile, used for the HumanSL model.
/// </summary>
public sealed partial record KataGoOverrideSettings
{
  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoOverrideSettings"/> class.
  /// </summary>
  /// <param name="botStrength">The bot strength formatted as Kyu20, Dan9 etc.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="botStrength"/> doesn't match a valid Kyu/Dan format.
  /// </exception>
  public KataGoOverrideSettings(string botStrength)
  {
    HumanSLProfile = MapBotStrength(botStrength);
  }

  /// <summary>
  /// Gets the HumanSLProfile to pass.
  /// </summary>
  public string HumanSLProfile { get; }

  [GeneratedRegex(@"^(Kyu(20|1[0-9]|[1-9])|Dan[1-9])$")]
  private static partial Regex Pattern();

  private static string MapBotStrength(string botStrength)
  {
    if (!Pattern().IsMatch(botStrength))
    {
      throw new ArgumentOutOfRangeException(nameof(botStrength), "Invalid bot strength");
    }

    int num = int.Parse(botStrength.AsSpan(3));
    char system = botStrength[..3] == "Kyu" ? 'k' : 'd';

    return $"rank_{num}{system}";
  }
}