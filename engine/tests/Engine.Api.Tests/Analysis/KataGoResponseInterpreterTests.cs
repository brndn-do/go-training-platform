using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public class KataGoResponseInterpreterTests()
{
  [Theory]
  [InlineData("test error", true, 0.5, true)]
  [InlineData(null, false, 0.5, true)]
  [InlineData(null, true, null, true)]
  [InlineData(null, true, 0.5, false)] // 83 - 1 = 82 is not a perfect square
  public void Interpet_InvalidResponseSuperhumanStrength_ThrowsArgumentException(
    string? error,
    bool includePolicy,
    double? winRate,
    bool useValidBoardLength)
  {
    KataGoRootInfo? rootInfo = winRate is null ? null : new((double)winRate, null);
    double[]? policy = null;
    if (includePolicy)
    {
      if (useValidBoardLength)
      {
        policy = [.1, .1, .1, .1, .1]; // 2 * 2 + 1
      }
      else
      {
        policy = [.1, .1, .1, .1];
      }
    }

    KataGoResponse response = new("test", error, policy, null, rootInfo);

    Assert.Throws<ArgumentException>(() => KataGoResponseInterpreter.Interpret(response, "Superhuman", new Random()));
  }

  [Theory]
  [InlineData("test error", true, 0.5, true)]
  [InlineData(null, false, 0.5, true)]
  [InlineData(null, true, null, true)]
  [InlineData(null, true, 0.5, false)] // 83 - 1 = 82 is not a perfect square
  public void Interpret_InvalidResponseHumanStrength_ThrowsArgumentException(
    string? error,
    bool includeHumanPolicy,
    double? humanWinRate,
    bool useValidBoardLength)
  {
    KataGoRootInfo? rootInfo = humanWinRate is null ? null : new(0.5, (double)humanWinRate);
    double[]? humanPolicy = null;
    if (includeHumanPolicy)
    {
      if (useValidBoardLength)
      {
        humanPolicy = [.1, .1, .1, .1, .1]; // 2 * 2 + 1
      }
      else
      {
        humanPolicy = [.1, .1, .1, .1];
      }
    }

    KataGoResponse response = new("test", error, [], humanPolicy, rootInfo);

    Assert.Throws<ArgumentException>(() => KataGoResponseInterpreter.Interpret(response, "Kyu5", new Random()));
  }

  [Theory]
  [InlineData(new double[] { .01, .01, .01, .01, .01, .01, .9, .01, .01, .01 }, 0, 0)] // index 6 -> A1
  [InlineData(new double[] { .01, .01, .01, .01, .01, .01, .01, .01, .9, .01 }, 2, 0)] // index 8 -> C1
  [InlineData(new double[] { .9, .01, .01, .01, .01, .01, .01, .01, .01, .01 }, 0, 2)] // index 0 -> A3
  [InlineData(new double[] { .01, .01, .9, .01, .01, .01, .01, .01, .01, .01 }, 2, 2)] // index 2 -> C3
  [InlineData(new double[] { .01, .01, .01, .01, .01, .01, .01, .9, .01, .01 }, 1, 0)] // index 7 -> B1
  [InlineData(new double[] { .01, .01, .01, .9, .01, .01, .01, .01, .01, .01 }, 0, 1)] // index 3 -> A2
  public void Interpret_SuperhumanStrength_ReturnsExpectedCoordinates(double[] policy, int expectedX, int expectedY)
  {
    KataGoRootInfo rootInfo = new(0.5, null);
    KataGoResponse response = new("test", null, policy, null, rootInfo);

    var (coordinates, winRate) = KataGoResponseInterpreter.Interpret(response, "Superhuman", new Random());

    Assert.Equal((expectedX, expectedY), coordinates);
    Assert.Equal(0.5, winRate);
  }

  [Fact]
  public void Interpret_SuperhumanStrength_MaxAtLastIndex_ReturnsPass()
  {
    // Last index (9, for a 3x3+1 array) represents pass.
    double[] policy = [.01, .01, .01, .01, .01, .01, .01, .01, .01, .9];
    KataGoRootInfo rootInfo = new(0.5, null);
    KataGoResponse response = new("test", null, policy, null, rootInfo);

    var (coordinates, winRate) = KataGoResponseInterpreter.Interpret(response, "Superhuman", new Random());

    Assert.Null(coordinates);
    Assert.Equal(0.5, winRate);
  }

  [Fact]
  public void Interpret_SuperhumanStrength_IllegalMovesMarkedNegativeOne_NeverWinArgMax()
  {
    // Every entry except index 2 (-> (2, 2), per the table above) is marked illegal.
    double[] policy = [-1, -1, .5, -1, -1, -1, -1, -1, -1, -1];
    KataGoRootInfo rootInfo = new(0.5, null);
    KataGoResponse response = new("test", null, policy, null, rootInfo);

    var (coordinates, winRate) = KataGoResponseInterpreter.Interpret(response, "Superhuman", new Random());

    Assert.Equal((2, 2), coordinates);
    Assert.Equal(0.5, winRate);
  }

  // Every entry except one is -1 (illegal), so sampling has only one possible outcome regardless
  // of which Random is used
  [Theory]
  [InlineData(new double[] { -1, -1, -1, -1, -1, -1, .5, -1, -1, -1 }, 0, 0)] // index 6 -> A1
  [InlineData(new double[] { -1, -1, -1, -1, -1, -1, -1, -1, .5, -1 }, 2, 0)] // index 8 -> C1
  [InlineData(new double[] { .5, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, 0, 2)] // index 0 -> A3
  [InlineData(new double[] { -1, -1, .5, -1, -1, -1, -1, -1, -1, -1 }, 2, 2)] // index 2 -> C3
  [InlineData(new double[] { -1, -1, -1, -1, -1, -1, -1, .5, -1, -1 }, 1, 0)] // index 7 -> B1
  [InlineData(new double[] { -1, -1, -1, .5, -1, -1, -1, -1, -1, -1 }, 0, 1)] // index 3 -> A2
  public void Interpret_HumanStrength_SingleLegalMove_AlwaysSelectsItRegardlessOfSeed(
    double[] humanPolicy, int expectedX, int expectedY)
  {
    KataGoRootInfo rootInfo = new(0.5, 0.6);
    KataGoResponse response = new("test", null, [], humanPolicy, rootInfo);

    var (coordinatesA, winRateA) = KataGoResponseInterpreter.Interpret(response, "Kyu5", new Random(1));
    var (coordinatesB, winRateB) = KataGoResponseInterpreter.Interpret(response, "Kyu5", new Random(999));

    Assert.Equal((expectedX, expectedY), coordinatesA);
    Assert.Equal((expectedX, expectedY), coordinatesB);
    Assert.Equal(0.6, winRateA);
    Assert.Equal(0.6, winRateB);
  }

  [Fact]
  public void Interpret_HumanStrength_SingleLegalMoveAtLastIndex_AlwaysReturnsPassRegardlessOfSeed()
  {
    // Last index (9, for a 3x3+1 array) represents pass.
    double[] humanPolicy = [-1, -1, -1, -1, -1, -1, -1, -1, -1, .5];
    KataGoRootInfo rootInfo = new(0.5, 0.6);
    KataGoResponse response = new("test", null, [], humanPolicy, rootInfo);

    var (coordinatesA, _) = KataGoResponseInterpreter.Interpret(response, "Kyu5", new Random(1));
    var (coordinatesB, _) = KataGoResponseInterpreter.Interpret(response, "Kyu5", new Random(999));

    Assert.Null(coordinatesA);
    Assert.Null(coordinatesB);
  }

  [Theory]
  [InlineData("Kyu21")] // above the valid 1-20 kyu range
  [InlineData("Kyu0")] // below the valid 1-20 kyu range
  [InlineData("Dan10")] // above the valid 1-9 dan range
  [InlineData("Dan0")] // below the valid 1-9 dan range
  [InlineData("")]
  [InlineData("foo")]
  [InlineData("kyu5")] // wrong case
  public void Interpret_InvalidBotStrength_ThrowsArgumentOutOfRangeException(string botStrength)
  {
    KataGoRootInfo rootInfo = new(0.5, 0.5);
    KataGoResponse response = new("test", null, [], [], rootInfo);

    Assert.Throws<ArgumentOutOfRangeException>(() => KataGoResponseInterpreter.Interpret(response, botStrength, new Random()));
  }
}