using Engine.Api.Analysis;
using Engine.Api.Processes;
using Microsoft.Extensions.Options;

namespace Engine.Api.Tests.Analysis;

/// <summary>
/// Exercises <see cref="SuggestionService"/> against a real <see cref="KataGoClient"/> wrapping
/// the real, gitignored katago binary and models — proving <see cref="KataGoQuery"/>'s actual
/// serialized output is accepted by KataGo, and that <see cref="KataGoResponseInterpreter"/>
/// correctly parses a response, not a hand-typed one. The binary and models are machine-local,
/// absent from a fresh clone and from CI, so skip these with <c>scripts/test-engine.sh --unit</c>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "KataGo")]
[Collection("KataGo")]
public sealed class SuggestionServiceIntegrationTests
{
  private const int BoardSize = 9;

  // have all tasks time out so tests don't hang, chain tasks with .WaitAsync(_timeout)
  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

  [Fact]
  public async Task GetSuggestionAsync_SuperhumanStrengthWithMoveHistory_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    // Black (2,2), White passes, Black (6,6)
    IReadOnlyList<Move?> moveHistory = [new Move(2, 2), null, new Move(6, 6)];

    var (move, blackWinRate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Superhuman")
      .WaitAsync(_timeout);

    Assert.InRange(blackWinRate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  [Fact]
  public async Task GetSuggestionAsync_KyuStrengthEmptyBoard_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    IReadOnlyList<Move?> moveHistory = [];

    var (move, blackWinRate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Kyu5")
      .WaitAsync(_timeout);

    Assert.InRange(blackWinRate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  [Fact]
  public async Task GetSuggestionAsync_DanStrengthEmptyBoard_ReturnsLegalSuggestion()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    await using var client = new KataGoClient(processIO, Options.Create(new KataGoClientOptions()));
    SuggestionService service = new(client, new Random());

    IReadOnlyList<Move?> moveHistory = [];

    var (move, blackWinRate) = await service.GetSuggestionAsync(moveHistory, BoardSize, 7.5, "Dan5")
      .WaitAsync(_timeout);

    Assert.InRange(blackWinRate, 0.0, 1.0);

    if (move is not null)
    {
      Assert.InRange(move.X, 0, BoardSize - 1);
      Assert.InRange(move.Y, 0, BoardSize - 1);
    }
  }

  private static IOptions<KataGoProcessIOOptions> GetProcessIOOptions()
  {
    return Options.Create(new KataGoProcessIOOptions());
  }

  private static IOptions<KataGoProcessOptions> GetProcessOptions()
  {
    string executablePath = Environment.GetEnvironmentVariable("KataGoProcess__ExecutablePath")
      ?? throw new InvalidOperationException("Set KataGoProcess__ExecutablePath to run this test.");
    string modelPath = Environment.GetEnvironmentVariable("KataGoProcess__ModelPath")
      ?? throw new InvalidOperationException("Set KataGoProcess__ModelPath to run this test.");
    string humanModelPath = Environment.GetEnvironmentVariable("KataGoProcess__HumanModelPath")
      ?? throw new InvalidOperationException("Set KataGoProcess__HumanModelPath to run this test.");
    string configPath = Environment.GetEnvironmentVariable("KataGoProcess__ConfigPath")
      ?? throw new InvalidOperationException("Set KataGoProcess__ConfigPath to run this test.");

    return Options.Create(new KataGoProcessOptions
    {
      ExecutablePath = executablePath,
      ConfigPath = configPath,
      ModelPath = modelPath,
      HumanModelPath = humanModelPath,
    });
  }
}
