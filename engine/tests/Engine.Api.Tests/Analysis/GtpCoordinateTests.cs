using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public sealed class GtpCoordinateTests()
{
  [Theory]
  [InlineData(-1)]
  [InlineData(19)]
  public void ToGtp_InvalidX_ThrowsArgumentOutOfRangeException(int x)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => GtpCoordinate.ToGtp(x, 0));
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(19)]
  public void ToGtp_InvalidY_ThrowsArgumentOutOfRangeException(int y)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => GtpCoordinate.ToGtp(0, y));
  }

  [Theory]
  [InlineData(0, 0, "A1")]
  [InlineData(18, 18, "T19")]
  [InlineData(7, 0, "H1")]
  [InlineData(8, 0, "J1")] // column after H skips "I"
  [InlineData(3, 4, "D5")]
  public void ToGtp_ValidCoordinates_ReturnsExpectedGtpString(int x, int y, string expected)
  {
    Assert.Equal(expected, GtpCoordinate.ToGtp(x, y));
  }

  [Theory]
  [InlineData("A1", 0, 0)]
  [InlineData("T19", 18, 18)]
  [InlineData("H1", 7, 0)]
  [InlineData("J1", 8, 0)] // column after H skips "I"
  [InlineData("D5", 3, 4)]
  public void FromGtp_ValidGtpString_ReturnsExpectedCoordinates(string gtp, int expectedX, int expectedY)
  {
    (int x, int y) = GtpCoordinate.FromGtp(gtp);

    Assert.Equal(expectedX, x);
    Assert.Equal(expectedY, y);
  }

  [Theory]
  [InlineData("I5")] // "I" is skipped, never a valid column
  [InlineData("U1")] // beyond "T", the last valid column
  [InlineData("A")] // missing row number
  [InlineData("AX")] // non-numeric row
  [InlineData("A0")] // row below the valid 1-19 range
  [InlineData("A20")] // row above the valid 1-19 range
  public void FromGtp_InvalidGtpString_ThrowsFormatException(string gtp)
  {
    Assert.Throws<FormatException>(() => GtpCoordinate.FromGtp(gtp));
  }
}
