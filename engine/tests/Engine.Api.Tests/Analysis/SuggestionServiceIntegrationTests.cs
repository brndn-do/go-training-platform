using Engine.Api.Analysis;
using Engine.Api.Processes;
using Microsoft.Extensions.Options;

namespace Engine.Api.Tests.Analysis;

/// <summary>
/// Exercises <see cref="SuggestionService"/> against a real <see cref="KataGoClient"/> wrapping
/// the real, gitignored katago binary and models — proving <see cref="KataGoQuery"/>'s actual
/// serialized output is accepted by KataGo, and that <see cref="KataGoResponseInterpreter"/>
/// correctly parses a genuine response, not a hand-typed stand-in. Excluded from the default
/// test run (see the "Category" trait) since those files are machine-local, not present in a
/// fresh clone or CI. Run explicitly with: <c>dotnet test --filter "Category=Integration"</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SuggestionServiceIntegrationTests
{
  private const int BoardSize = 9;

  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

  [Fact]
  public async Task GetSuggestionAsync_SuperhumanStrengthWithMoveHistory_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    // Black (2,2), White passes, Black (6,6)
    IReadOnlyList<Move?> moveHistory = [new Move(2, 2), null, new Move(6, 6)];

    var (move, winrate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Superhuman")
      .WaitAsync(_timeout);

    Assert.InRange(winrate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  [Fact]
  public async Task GetSuggestionAsync_KyuStrengthEmptyBoard_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    IReadOnlyList<Move?> moveHistory = [];

    var (move, winrate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Kyu5")
      .WaitAsync(_timeout);

    Assert.InRange(winrate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  [Fact]
  public async Task GetSuggestionAsync_DanStrengthEmptyBoard_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    IReadOnlyList<Move?> moveHistory = [];

    var (move, winrate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Dan5")
      .WaitAsync(_timeout);

    Assert.InRange(winrate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  private static IOptions<KataGoProcessOptions> GetOptions()
  {
    string binaryPath = Environment.GetEnvironmentVariable("KataGo__BinaryPath")
      ?? throw new InvalidOperationException("Set KataGo__BinaryPath to run this test.");
    string modelPath = Environment.GetEnvironmentVariable("KataGo__ModelPath")
      ?? throw new InvalidOperationException("Set KataGo__ModelPath to run this test.");
    string humanModelPath = Environment.GetEnvironmentVariable("KataGo__HumanModelPath")
      ?? throw new InvalidOperationException("Set KataGo__HumanModelPath to run this test.");
    string configPath = Environment.GetEnvironmentVariable("KataGo__ConfigPath")
      ?? throw new InvalidOperationException("Set KataGo__ConfigPath to run this test.");

    return Options.Create(new KataGoProcessOptions
    {
      BinaryPath = binaryPath,
      ConfigPath = configPath,
      ModelPath = modelPath,
      HumanModelPath = humanModelPath,
    });
  }
}
