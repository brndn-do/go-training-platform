using static Engine.Api.Extensions.DoubleExtensions;

namespace Engine.Api.Tests.Extensions;

public sealed class DoubleExtensionsTests
{
  [Theory]
  [InlineData(-.75)]
  [InlineData(.75)]
  [InlineData(4.44)]
  [InlineData(3.14)]
  [InlineData(0.5 + 1e-9)]
  [InlineData(0.5 - 1e-9)]
  public void IsIntegerOrHalfInteger_NonIntegerOrHalfInteger_ReturnsFalse(double val)
  {
    Assert.False(IsIntegerOrHalfInteger(val));
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(1)]
  public void IsIntegerOrHalfInteger_WholeInteger_ReturnsTrue(double val)
  {
    Assert.True(IsIntegerOrHalfInteger(val));
  }

  [Theory]
  [InlineData(-.5)]
  [InlineData(0.5)]
  [InlineData(1.5)]
  public void IsIntegerOrHalfInteger_HalfInteger_ReturnsTrue(double val)
  {
    Assert.True(IsIntegerOrHalfInteger(val));
  }
}