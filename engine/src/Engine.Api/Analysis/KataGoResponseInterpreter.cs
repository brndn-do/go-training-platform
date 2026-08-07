using System.Text.RegularExpressions;
using MathNet.Numerics.Distributions;

namespace Engine.Api.Analysis;

/// <summary>
/// Interprets a <see cref="KataGoResponse"/> into a chosen move and win rate. Superhuman
/// strength picks the single best move via argmax over <see cref="KataGoResponse.Policy"/>;
/// human-ranked strengths sample proportionally from <see cref="KataGoResponse.HumanPolicy"/>.
/// </summary>
public static partial class KataGoResponseInterpreter
{
  private const string Superhuman = "Superhuman";

  /// <summary>
  /// Interprets a <see cref="KataGoResponse"/> for the given bot strength.
  /// </summary>
  /// <param name="response">The response to interpret.</param>
  /// <param name="botStrength">The bot strength formatted as Kyu20, Dan9, Superhuman, etc.</param>
  /// <param name="random">The source of randomness used for proportional sampling at human-ranked strengths.</param>
  /// <returns>The chosen move (<c>null</c> for pass) and the resulting win rate.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="response"/> is an error response, or its policy, win rate, or
  /// policy length is invalid for the requested <paramref name="botStrength"/>.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when <paramref name="botStrength"/> doesn't match a valid Kyu/Dan format.
  /// </exception>
  public static ((int X, int Y)? Coordinates, double Winrate) Interpret(
    KataGoResponse response,
    string botStrength,
    Random random)
  {
    if (response.IsError)
    {
      throw new ArgumentException("Invalid response.", nameof(response));
    }

    if (botStrength == Superhuman)
    {
      return Interpret(true, response.Policy, response.RootInfo?.Winrate, random);
    }

    if (!Pattern().IsMatch(botStrength))
    {
      throw new ArgumentOutOfRangeException(nameof(botStrength), "Invalid bot strength.");
    }

    return Interpret(false, response.HumanPolicy, response.RootInfo?.HumanWinrate, random);
  }

  // private helper, specify whether to use argmax or sample proportionally, pass in policy and win rate to use
  private static ((int X, int Y)? Coordinates, double Winrate) Interpret(
    bool useArgMax,
    double[]? policyToUse,
    double? winRateToUse,
    Random random)
  {
    if (policyToUse is null)
    {
      throw new ArgumentException("The provided policy to use is null.", nameof(policyToUse));
    }

    if (winRateToUse is null)
    {
      throw new ArgumentException("The provided win rate to use is null.", nameof(winRateToUse));
    }

    double tryGetBoardSize = Math.Sqrt(policyToUse.Length - 1);
    if (tryGetBoardSize != Math.Floor(tryGetBoardSize))
    {
      throw new ArgumentException("The provided policy has an invalid length.", nameof(policyToUse));
    }

    int boardSize = (int)tryGetBoardSize;

    int i = useArgMax
      ? Array.IndexOf(policyToUse, policyToUse.Max()) // get the max
      : new Categorical([.. policyToUse.Select(x => x < 0 ? 0 : x)], random)
        .Sample(); // change -1's to 0's then sample proportionally

    (int X, int Y)? coordinates = i < policyToUse.Length - 1 // not the last element, represents a coordinate on board
      ? (i % boardSize, boardSize - 1 - (i / boardSize)) // KataGo returns top row first, so flip the y-axis
      : null; // last index means a pass

    return (coordinates, (double)winRateToUse);
  }

  [GeneratedRegex(@"^(Kyu(20|1[0-9]|[1-9])|Dan[1-9])$")]
  private static partial Regex Pattern();
}