using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public sealed class KataGoOverrideSettingsTests()
{
  [Theory]
  [InlineData("Kyu5", "rank_5k")]
  [InlineData("Kyu20", "rank_20k")]
  [InlineData("Kyu1", "rank_1k")]
  [InlineData("Dan1", "rank_1d")]
  [InlineData("Dan9", "rank_9d")]
  public void Constructor_ValidBotStrength_MapsToExpectedHumanSLProfile(string botStrength, string expected)
  {
    var settings = new KataGoOverrideSettings(botStrength);

    Assert.Equal(expected, settings.HumanSLProfile);
  }

  [Theory]
  [InlineData("Kyu21")] // above the valid 1-20 kyu range
  [InlineData("Kyu0")] // below the valid 1-20 kyu range
  [InlineData("Dan10")] // above the valid 1-9 dan range
  [InlineData("Dan0")] // below the valid 1-9 dan range
  [InlineData("")]
  [InlineData("foo")]
  [InlineData("kyu5")] // wrong case
  [InlineData("Superhuman")] // KataGoQuery never passes this, but the constructor should still reject it on its own
  public void Constructor_InvalidBotStrength_ThrowsArgumentOutOfRangeException(string botStrength)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new KataGoOverrideSettings(botStrength));
  }
}
