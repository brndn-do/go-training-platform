namespace GoTrainingPlatform.Domain.Enums;

/// <summary>
/// The strength of the bot a game is played against, from the weakest rank
/// (<see cref="Kyu20"/>) through the strongest rank (<see cref="Dan9"/>), plus one level
/// beyond any rank (<see cref="Superhuman"/>).
/// </summary>
public enum BotStrength
{
  /// <summary>20 kyu — the weakest rank.</summary>
  Kyu20,

  /// <summary>19 kyu.</summary>
  Kyu19,

  /// <summary>18 kyu.</summary>
  Kyu18,

  /// <summary>17 kyu.</summary>
  Kyu17,

  /// <summary>16 kyu.</summary>
  Kyu16,

  /// <summary>15 kyu.</summary>
  Kyu15,

  /// <summary>14 kyu.</summary>
  Kyu14,

  /// <summary>13 kyu.</summary>
  Kyu13,

  /// <summary>12 kyu.</summary>
  Kyu12,

  /// <summary>11 kyu.</summary>
  Kyu11,

  /// <summary>10 kyu.</summary>
  Kyu10,

  /// <summary>9 kyu.</summary>
  Kyu9,

  /// <summary>8 kyu.</summary>
  Kyu8,

  /// <summary>7 kyu.</summary>
  Kyu7,

  /// <summary>6 kyu.</summary>
  Kyu6,

  /// <summary>5 kyu.</summary>
  Kyu5,

  /// <summary>4 kyu.</summary>
  Kyu4,

  /// <summary>3 kyu.</summary>
  Kyu3,

  /// <summary>2 kyu.</summary>
  Kyu2,

  /// <summary>1 kyu.</summary>
  Kyu1,

  /// <summary>1 dan.</summary>
  Dan1,

  /// <summary>2 dan.</summary>
  Dan2,

  /// <summary>3 dan.</summary>
  Dan3,

  /// <summary>4 dan.</summary>
  Dan4,

  /// <summary>5 dan.</summary>
  Dan5,

  /// <summary>6 dan.</summary>
  Dan6,

  /// <summary>7 dan.</summary>
  Dan7,

  /// <summary>8 dan.</summary>
  Dan8,

  /// <summary>9 dan — the strongest rank.</summary>
  Dan9,

  /// <summary>Maximum strength, beyond any ranked level.</summary>
  Superhuman,
}
