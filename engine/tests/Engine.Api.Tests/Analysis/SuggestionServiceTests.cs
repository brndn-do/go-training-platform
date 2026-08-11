using Engine.Api.Analysis;
using Engine.Api.Tests.Fakes;

namespace Engine.Api.Tests.Analysis;

public sealed class SuggestionServiceTests()
{
  [Fact]
  public async Task GetSuggestionAsync_SuperhumanStrength_ReturnsInterpretedSuggestion()
  {
    double[] policy = [.01, .01, .01, .01, .01, .01, .9, .01, .01, .01]; // index 6 -> A1 -> (0, 0)
    KataGoRootInfo rootInfo = new(0.5, null);
    KataGoResponse response = new("test", null, policy, null, rootInfo);

    var (coordinates, winRate) = await Service(response).GetSuggestionAsync([(1, 1)], 3, 7.5, "Superhuman");

    Assert.Equal((0, 0), coordinates);
    Assert.Equal(0.5, winRate);
  }

  [Fact]
  public async Task GetSuggestionAsync_RankedStrength_ReturnsInterpretedSuggestion()
  {
    // Every entry except index 6 is illegal, so sampling has only one possible outcome
    // regardless of which Random each SuggestionService was constructed with.
    double[] humanPolicy = [-1, -1, -1, -1, -1, -1, .5, -1, -1, -1]; // index 6 -> A1 -> (0, 0)
    KataGoRootInfo rootInfo = new(0.5, 0.6);
    KataGoResponse response = new("test", null, [], humanPolicy, rootInfo);

    var (coordinatesA, winRateA) = await Service(response).GetSuggestionAsync([(1, 1)], 3, 7.5, "Kyu5");
    var (coordinatesB, winRateB) = await Service(response).GetSuggestionAsync([(1, 1)], 3, 7.5, "Kyu5");

    Assert.Equal((0, 0), coordinatesA);
    Assert.Equal((0, 0), coordinatesB);
    Assert.Equal(0.6, winRateA);
    Assert.Equal(0.6, winRateB);
  }

  [Fact]
  public async Task GetSuggestionAsync_GivenInputs_BuildsExpectedQuery()
  {
    double[] humanPolicy = [-1, -1, -1, -1, -1, -1, .5, -1, -1, -1];
    KataGoRootInfo rootInfo = new(0.5, 0.6);
    KataGoResponse response = new("test", null, [], humanPolicy, rootInfo);

    FakeKataGoClient fakeKataGoClient = new(response);
    SuggestionService service = new(fakeKataGoClient, new Random());

    IReadOnlyList<(int X, int Y)?> moveHistory = [(1, 1), null, (0, 2)];

    await service.GetSuggestionAsync(moveHistory, 3, 6.5, "Kyu5");

    KataGoQuery? query = fakeKataGoClient.QueryReceived;
    Assert.NotNull(query);
    Assert.Equal(3, query.BoardXSize);
    Assert.Equal(3, query.BoardYSize);
    Assert.Equal(6.5, query.Komi);

    Assert.Equal("B", query.Moves[0][0]);
    Assert.Equal("B2", query.Moves[0][1]); // (1, 1) -> B2
    Assert.Equal("W", query.Moves[1][0]);
    Assert.Equal("pass", query.Moves[1][1]);
    Assert.Equal("B", query.Moves[2][0]);
    Assert.Equal("A3", query.Moves[2][1]); // (0, 2) -> A3

    Assert.NotNull(query.OverrideSettings);
    Assert.Equal("rank_5k", query.OverrideSettings.HumanSLProfile);
  }

  // returns a suggestion service that uses the fixed KataGo response provided
  private static SuggestionService Service(KataGoResponse response) =>
    new(new FakeKataGoClient(response), new Random());
}
