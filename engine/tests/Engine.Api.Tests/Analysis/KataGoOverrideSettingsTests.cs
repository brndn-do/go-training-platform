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
    var settings = new KataGoOverrideSettings(new BotStrength(botStrength));

    Assert.Equal(expected, settings.HumanSLProfile);
  }

  [Fact]
  public void Constructor_SuperhumanBotStrength_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new KataGoOverrideSettings(new BotStrength("Superhuman")));
  }
}
