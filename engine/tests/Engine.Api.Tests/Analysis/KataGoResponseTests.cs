using System.Text.Json;
using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public sealed class KataGoResponseTests()
{
  private static readonly JsonSerializerOptions _options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  [Fact]
  public void Deserialize_SuccessfulResponseWithoutHumanSLProfile_ReturnsExpectedValues()
  {
    string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Analysis", "TestData", "exampleResponseNoHumanSL.json"));

    KataGoResponse? response = JsonSerializer.Deserialize<KataGoResponse>(json, _options);

    Assert.NotNull(response);
    Assert.Equal("test", response.Id);
    Assert.False(response.IsError);
    Assert.Null(response.Error);

    Assert.NotNull(response.Policy);
    Assert.Equal(362, response.Policy.Length); // 19 * 19 + 1 (pass)
    Assert.Null(response.HumanPolicy);

    Assert.NotNull(response.RootInfo);
    Assert.Equal(0.16601906, response.RootInfo.Winrate); // catches Winrate silently binding to the wrong property
    Assert.Null(response.RootInfo.HumanWinrate);
  }

  [Fact]
  public void Deserialize_SuccessfulResponseWithHumanSLProfile_ReturnsExpectedValues()
  {
    string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Analysis", "TestData", "exampleResponseWithHumanSL.json"));

    KataGoResponse? response = JsonSerializer.Deserialize<KataGoResponse>(json, _options);

    Assert.NotNull(response);
    Assert.Equal("test", response.Id);
    Assert.False(response.IsError);
    Assert.Null(response.Error);

    Assert.NotNull(response.Policy);
    Assert.Equal(362, response.Policy.Length); // 19 * 19 + 1 (pass)
    Assert.NotNull(response.HumanPolicy);
    Assert.NotEqual(response.Policy[0], response.HumanPolicy[0]);

    Assert.NotNull(response.RootInfo);
    Assert.NotNull(response.RootInfo.HumanWinrate);
    Assert.NotEqual(response.RootInfo.Winrate, response.RootInfo.HumanWinrate.Value);
  }

  [Fact]
  public void Deserialize_RejectedQuery_ReturnsExpectedValues()
  {
    string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Analysis", "TestData", "exampleResponseRejected.json"));

    KataGoResponse? response = JsonSerializer.Deserialize<KataGoResponse>(json, _options);

    Assert.NotNull(response);
    Assert.Equal("test", response.Id);
    Assert.True(response.IsError);
    Assert.Equal("Must provide an integer from 2 to 19", response.Error);

    Assert.Null(response.Policy);
    Assert.Null(response.HumanPolicy);
    Assert.Null(response.RootInfo);
  }
}
