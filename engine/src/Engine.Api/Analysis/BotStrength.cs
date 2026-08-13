using System.Text.RegularExpressions;

namespace Engine.Api.Analysis;

/// <summary>
/// A validated bot strength: either "Superhuman", or a Kyu/Dan rank formatted as Kyu1-Kyu20 or
/// Dan1-Dan9.
/// </summary>
public sealed partial record BotStrength
{
  private const string SuperhumanValue = "Superhuman";

  /// <summary>
  /// Initializes a new instance of the <see cref="BotStrength"/> class.
  /// </summary>
  /// <param name="value">The bot strength formatted as Kyu20, Dan9, Superhuman, etc.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="value"/> is neither "Superhuman" nor a valid Kyu/Dan format.
  /// </exception>
  public BotStrength(string value)
  {
    if (value != SuperhumanValue && !Pattern().IsMatch(value))
    {
      throw new ArgumentOutOfRangeException(nameof(value), "Invalid bot strength.");
    }

    Value = value;
  }

  /// <summary>
  /// Gets the bot strength formatted as Kyu20, Dan9, Superhuman, etc.
  /// </summary>
  public string Value { get; }

  /// <summary>
  /// Gets a value indicating whether this is Superhuman strength.
  /// </summary>
  public bool IsSuperhuman => Value == SuperhumanValue;

  [GeneratedRegex(@"^(Kyu(20|1[0-9]|[1-9])|Dan[1-9])$")]
  private static partial Regex Pattern();
}
