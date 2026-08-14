using System.Diagnostics;
using Engine.Api.Processes;
using Microsoft.Extensions.Options;

namespace Engine.Api.Tests.Processes;

/// <summary>
/// Exercises <see cref="KataGoProcessIO"/> against the real, gitignored katago binary and
/// models. Excluded from the default test run (see the "Category" trait) since those files
/// are machine-local, not present in a fresh clone or CI. Run explicitly with:
/// <c>dotnet test --filter "Category=Integration"</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class KataGoProcessIOIntegrationTests
{
  // have all tasks time out after 30 seconds so tests don't hang
  // chain tasks with .WaitAsync(_timeout)
  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

  [Fact]
  public async Task ExchangeAsync_SingleSuperhumanQuery_ReturnsCorrespondingResponse()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test\"", result);
    Assert.DoesNotContain("\"error\"", result);
  }

  // tests that human model loaded as well
  [Fact]
  public async Task ExchangeAsync_SingleHumanQuery_ReturnsCorrespondingResponse()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test\"", result);
    Assert.DoesNotContain("\"error\"", result);
  }

  [Fact]
  public async Task ExchangeAsync_SequentialQueries_ReturnsCorrespondingResponses()
  {
    string query1 = """{"id":"test1","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    string query2 = """{"id":"test2","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    var result1 = await processIO.ExchangeAsync(query1).WaitAsync(_timeout);
    var result2 = await processIO.ExchangeAsync(query2).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test1\"", result1);
    Assert.DoesNotContain("\"error\"", result1);
    Assert.Contains("\"id\":\"test2\"", result2);
    Assert.DoesNotContain("\"error\"", result2);
  }

  [Fact]
  public async Task WarmUpAsync_FreshProcess_CompletesOnceReady()
  {
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    Task warmUpTask = processIO.WarmUpAsync();
    await warmUpTask.WaitAsync(_timeout);

    Assert.True(warmUpTask.IsCompleted);
  }

  [Fact]
  public async Task WarmUpAsync_AlreadyWarm_CompletesImmediately()
  {
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    Task warmUpTask1 = processIO.WarmUpAsync();
    await warmUpTask1.WaitAsync(_timeout);

    Assert.True(warmUpTask1.IsCompleted);

    Task warmUpTask2 = processIO.WarmUpAsync();

    Assert.True(warmUpTask2.IsCompleted);

    await warmUpTask2.WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_CallerCancelled_ThrowsOperationCanceledException()
  {
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);
    using var cts = new CancellationTokenSource();

    var warmUpTask = processIO.WarmUpAsync(cts.Token);
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => warmUpTask).WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_ProcessExited_ThrowsInvalidOperationException()
  {
    var processOptions = GetOptions();
    processOptions.Value.ModelPath = "invalidpath";
    await using var processIO = new KataGoProcessIO(processOptions);

    await Assert.ThrowsAsync<InvalidOperationException>(() => processIO.WarmUpAsync()).WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_DisposedWhileWaiting_ThrowsInvalidOperationException()
  {
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions);

    var warmUpTask = processIO.WarmUpAsync();
    var disposeTask = processIO.DisposeAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => warmUpTask).WaitAsync(_timeout);
    await disposeTask.AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_CalledOnDisposedInstance_ThrowsObjectDisposedException()
  {
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions, shutdownGracePeriodMs: 1000);

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    await Assert.ThrowsAsync<ObjectDisposedException>(() => processIO.WarmUpAsync()).WaitAsync(_timeout);
  }

  [Fact]
  public async Task IsReady_FreshProcess_ReturnsFalse()
  {
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);

    Assert.False(processIO.IsReady);
  }

  [Fact]
  public async Task IsReady_AfterWarmUp_ReturnsTrue()
  {
    var processOptions = GetOptions();
    await using var processIO = new KataGoProcessIO(processOptions);

    await processIO.WarmUpAsync().WaitAsync(_timeout);

    Assert.True(processIO.IsReady);
  }

  [Fact]
  public async Task IsReady_CalledOnDisposedInstance_DoesNotThrow()
  {
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions);

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    Assert.False(processIO.IsReady);
  }

  [Fact]
  public async Task DisposeAsync_AlreadyDisposed_Succeeds()
  {
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions);

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task DisposeAsync_ProcessExited_Succeeds()
  {
    var processOptions = GetOptions();
    processOptions.Value.ModelPath = "invalidpath";
    var processIO = new KataGoProcessIO(processOptions);

    // wait until the process exits
    await Assert.ThrowsAsync<InvalidOperationException>(() => processIO.WarmUpAsync()).WaitAsync(_timeout);

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task DisposeAsync_AfterRealUse_ExitsGracefully()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions, shutdownGracePeriodMs: 1000);
    try
    {
      await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    }
    finally
    {
      var sw = Stopwatch.StartNew();
      await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
      Assert.True(sw.ElapsedMilliseconds < 1000, $"Expected a graceful exit under the grace period, took {sw.ElapsedMilliseconds}ms.");
    }
  }

  [Fact]
  public async Task DisposeAsync_ProcessStillStarting_ForcesKillAfterGracePeriod()
  {
    var processOptions = GetOptions();
    var processIO = new KataGoProcessIO(processOptions, shutdownGracePeriodMs: 1000);

    var sw = Stopwatch.StartNew();
    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    Assert.True(
      sw.ElapsedMilliseconds >= 1000,
      $"Expected the forceful-kill path (~1000ms+) that completes under 5000ms, took {sw.ElapsedMilliseconds}ms.");
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
