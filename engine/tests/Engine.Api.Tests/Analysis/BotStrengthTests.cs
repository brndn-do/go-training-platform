using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public sealed class BotStrengthTests()
{
  [Theory]
  [InlineData("Kyu5")]
  [InlineData("Kyu20")]
  [InlineData("Kyu1")]
  [InlineData("Dan1")]
  [InlineData("Dan9")]
  public void Constructor_ValidKyuDanFormat_SetsValueAndIsNotSuperhuman(string value)
  {
    var botStrength = new BotStrength(value);

    Assert.Equal(value, botStrength.Value);
    Assert.False(botStrength.IsSuperhuman);
  }

  [Fact]
  public void Constructor_Superhuman_SetsValueAndIsSuperhuman()
  {
    var botStrength = new BotStrength("Superhuman");

    Assert.Equal("Superhuman", botStrength.Value);
    Assert.True(botStrength.IsSuperhuman);
  }

  [Theory]
  [InlineData("Kyu21")] // above the valid 1-20 kyu range
  [InlineData("Kyu0")] // below the valid 1-20 kyu range
  [InlineData("Dan10")] // above the valid 1-9 dan range
  [InlineData("Dan0")] // below the valid 1-9 dan range
  [InlineData("")]
  [InlineData("foo")]
  [InlineData("kyu5")] // wrong case
  public void Constructor_InvalidFormat_ThrowsArgumentOutOfRangeException(string value)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new BotStrength(value));
  }
}
