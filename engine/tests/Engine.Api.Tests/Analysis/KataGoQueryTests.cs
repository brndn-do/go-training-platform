using System.Text.Json;
using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public sealed class KataGoQueryTests()
{
  private static readonly JsonSerializerOptions _options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  [Theory]
  [InlineData(1)] // below the valid 2-19 range
  [InlineData(20)] // above the valid 2-19 range
  public void Constructor_InvalidBoardSize_ThrowsArgumentOutOfRangeException(int boardSize)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new KataGoQuery("test", [], boardSize, 7.5, new BotStrength("Superhuman")));
  }

  [Theory]
  [InlineData(-151)] // below the valid -150 to 150 range
  [InlineData(151)] // above the valid -150 to 150 range
  public void Constructor_KomiOutOfRange_ThrowsArgumentOutOfRangeException(double komi)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new KataGoQuery("test", [], 19, komi, new BotStrength("Superhuman")));
  }

  [Fact]
  public void Constructor_KomiNotIntegerOrHalfInteger_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new KataGoQuery("test", [], 19, 7.3, new BotStrength("Superhuman")));
  }

  // An invalid botStrength format can no longer reach this constructor at all — BotStrength's
  // own constructor rejects it first (see BotStrengthTests).
  [Fact]
  public void Serialize_RankedQuery_ProducesExpectedJsonShape()
  {
    var query = new KataGoQuery("test", [new Move(4, 4), new Move(2, 2)], 19, 7.5, new BotStrength("Kyu5"));

    JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(query, _options));
    JsonElement root = doc.RootElement;

    Assert.Equal("test", root.GetProperty("id").GetString());
    Assert.Equal("chinese", root.GetProperty("rules").GetString());
    Assert.Equal(7.5, root.GetProperty("komi").GetDouble());
    Assert.True(root.GetProperty("includePolicy").GetBoolean());

    // Catches BoardXSize/BoardYSize serializing as "boardSizeX"/"boardSizeY" instead of
    // "boardXSize"/"boardYSize" (KataGo's actual field names).
    Assert.Equal(19, root.GetProperty("boardXSize").GetInt32());
    Assert.Equal(19, root.GetProperty("boardYSize").GetInt32());

    JsonElement moves = root.GetProperty("moves");
    Assert.Equal("B", moves[0][0].GetString());
    Assert.Equal("E5", moves[0][1].GetString());
    Assert.Equal("W", moves[1][0].GetString());
    Assert.Equal("C3", moves[1][1].GetString());

    JsonElement overrideSettings = root.GetProperty("overrideSettings");
    Assert.Equal("rank_5k", overrideSettings.GetProperty("humanSLProfile").GetString());

    // Catches BotStrength leaking into the JSON sent to KataGo, which has no such field.
    Assert.False(overrideSettings.TryGetProperty("botStrength", out _));
  }

  // The real KataGo analysis engine rejects an explicit "overrideSettings":null with
  // {"error":"Must be an object","field":"overrideSettings",...} — the key must be omitted
  // entirely, not present with a null value.
  [Fact]
  public void Serialize_SuperhumanQuery_OmitsOverrideSettings()
  {
    var query = new KataGoQuery("test", [], 19, 7.5, new BotStrength("Superhuman"));

    JsonDocument doc = JsonDocument.Parse(JsonSerializer.Serialize(query, _options));

    Assert.False(doc.RootElement.TryGetProperty("overrideSettings", out _));
  }
}
