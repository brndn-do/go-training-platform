namespace Engine.Api.Extensions;

/// <summary>
/// Provides extension methods for <see cref="double"/> values.
/// </summary>
public static class DoubleExtensions
{
  private const double Tolerance = 1e-9;

  /// <summary>
  /// Determines whether the value given is an integer or a half integer (.5).
  /// </summary>
  /// <param name="val">The value to check.</param>
  /// <returns><c>true</c> if <paramref name="val"/> is a whole integer or half integer,
  /// <c>false</c> otherwise.</returns>
  public static bool IsIntegerOrHalfInteger(double val)
  {
    double doubled = val * 2;
    return Math.Abs(doubled - Math.Round(doubled)) < Tolerance;
  }
}