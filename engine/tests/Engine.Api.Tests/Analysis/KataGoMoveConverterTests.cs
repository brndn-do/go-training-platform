using Engine.Api.Analysis;

namespace Engine.Api.Tests.Analysis;

public class KataGoMoveConverterTests()
{
  [Fact]
  public void Convert_EmptyHistory_ReturnsEmptyList()
  {
    IReadOnlyList<string[]> result = KataGoMoveConverter.Convert([]);

    Assert.Empty(result);
  }

  [Fact]
  public void Convert_SingleMove_ReturnsBlackMove()
  {
    IReadOnlyList<(int X, int Y)?> moves = [(4, 4)];

    IReadOnlyList<string[]> result = KataGoMoveConverter.Convert(moves);

    Assert.Equal(["B", "E5"], result[0]);
  }

  [Fact]
  public void Convert_MultipleMoves_AlternatesColorByIndex()
  {
    // Matches an earlier real query: [["B","E5"],["W","C3"],["B","G3"]]
    IReadOnlyList<(int X, int Y)?> moves = [(4, 4), (2, 2), (6, 2)];

    IReadOnlyList<string[]> result = KataGoMoveConverter.Convert(moves);

    Assert.Equal(["B", "E5"], result[0]);
    Assert.Equal(["W", "C3"], result[1]);
    Assert.Equal(["B", "G3"], result[2]);
  }

  [Fact]
  public void Convert_NullCoordinates_ReturnsPass()
  {
    IReadOnlyList<(int X, int Y)?> moves = [(4, 4), null];

    IReadOnlyList<string[]> result = KataGoMoveConverter.Convert(moves);

    Assert.Equal(["B", "E5"], result[0]);
    Assert.Equal(["W", "pass"], result[1]);
  }
}
