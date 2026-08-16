using MathNet.Numerics.Distributions;

namespace Engine.Api.Analysis;

/// <summary>
/// Interprets a <see cref="KataGoResponse"/> into a chosen move and win rate. Superhuman
/// strength picks the single best move via argmax over <see cref="KataGoResponse.Policy"/>;
/// human-ranked strengths sample proportionally from <see cref="KataGoResponse.HumanPolicy"/>.
/// </summary>
public static class KataGoResponseInterpreter
{
  /// <summary>
  /// Interprets a <see cref="KataGoResponse"/> for the given bot strength.
  /// </summary>
  /// <param name="response">The response to interpret.</param>
  /// <param name="botStrength">The bot strength.</param>
  /// <param name="random">The source of randomness used for proportional sampling at human-ranked strengths.</param>
  /// <returns>
  /// The chosen move (<c>null</c> for pass) and the resulting estimate of Black's win
  /// probability.
  /// </returns>
  /// <exception cref="InvalidKataGoResponseException">
  /// Thrown when <paramref name="response"/> is an error response, or its policy, win rate, or
  /// policy length is invalid for the requested <paramref name="botStrength"/>.
  /// </exception>
  public static (Move? Move, double BlackWinRate) Interpret(
    KataGoResponse response,
    BotStrength botStrength,
    Random random)
  {
    if (response.IsError)
    {
      throw new InvalidKataGoResponseException("KataGo returned an error");
    }

    if (botStrength.IsSuperhuman)
    {
      return Interpret(true, response.Policy, response.RootInfo?.BlackWinRate, random);
    }

    return Interpret(false, response.HumanPolicy, response.RootInfo?.HumanBlackWinRate, random);
  }

  // private helper, specify whether to use argmax or sample proportionally, pass in policy and win rate to use
  private static (Move? Move, double BlackWinRate) Interpret(
    bool useArgMax,
    double[]? policyToUse,
    double? winRateToUse,
    Random random)
  {
    if (policyToUse is null)
    {
      throw new InvalidKataGoResponseException("The provided policy to use is null.");
    }

    if (winRateToUse is null)
    {
      throw new InvalidKataGoResponseException("The provided win rate to use is null.");
    }

    double tryGetBoardSize = Math.Sqrt(policyToUse.Length - 1);
    if (tryGetBoardSize != Math.Floor(tryGetBoardSize))
    {
      throw new InvalidKataGoResponseException("The provided policy has an invalid length.");
    }

    int boardSize = (int)tryGetBoardSize;

    int i = useArgMax
      ? Array.IndexOf(policyToUse, policyToUse.Max()) // get the max
      : new Categorical([.. policyToUse.Select(x => x < 0 ? 0 : x)], random)
        .Sample(); // change -1's to 0's then sample proportionally

    Move? move = i < policyToUse.Length - 1 // not the last element, represents a coordinate on board
      ? new Move(i % boardSize, boardSize - 1 - (i / boardSize)) // KataGo returns top row first, so flip the y-axis
      : null; // last index means a pass

    return (move, (double)winRateToUse);
  }
}