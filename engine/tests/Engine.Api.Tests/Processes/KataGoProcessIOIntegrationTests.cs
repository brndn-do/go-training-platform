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
  // have all tasks time out after 180 seconds so tests don't hang
  // chain tasks with .WaitAsync(_timeout)
  private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(180);

  [Fact]
  public async Task ExchangeAsync_SingleSuperhumanQuery_ReturnsCorrespondingResponse()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test\"", result);
    Assert.DoesNotContain("\"error\"", result);
  }

  // tests that human model loaded as well
  [Fact]
  public async Task ExchangeAsync_SingleHumanQuery_ReturnsCorrespondingResponse()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test\"", result);
    Assert.DoesNotContain("\"error\"", result);
  }

  [Fact]
  public async Task ExchangeAsync_SequentialQueries_ReturnsCorrespondingResponses()
  {
    string query1 = """{"id":"test1","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    string query2 = """{"id":"test2","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    var result1 = await processIO.ExchangeAsync(query1).WaitAsync(_timeout);
    var result2 = await processIO.ExchangeAsync(query2).WaitAsync(_timeout);
    Assert.Contains("\"id\":\"test1\"", result1);
    Assert.DoesNotContain("\"error\"", result1);
    Assert.Contains("\"id\":\"test2\"", result2);
    Assert.DoesNotContain("\"error\"", result2);
  }

  [Fact]
  public async Task ExchangeAsync_ProcessHasExited_ReturnsNull()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    var processOptions = GetProcessOptions();
    processOptions.Value.ModelPath = "invalidpath";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), processOptions);

    // the process crashes asynchronously; poll rather than assert immediately
    using CancellationTokenSource cts = new(_timeout);
    while (!processIO.HasExited && !cts.IsCancellationRequested)
    {
      await Task.Delay(10);
    }

    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Null(result);
  }

  [Fact]
  public async Task ExchangeAsync_ProcessExitsDuringQuery_ReturnsNull()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    var processOptions = GetProcessOptions();
    processOptions.Value.ModelPath = "invalidpath";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), processOptions);

    // send a query before it has time to crash
    var result = await processIO.ExchangeAsync(query).WaitAsync(_timeout);
    Assert.Null(result);
  }

  [Fact]
  public async Task ExchangeAsync_CallerCancelled_ThrowsOperationCanceledException()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    using var cts = new CancellationTokenSource();

    // the process hasn't finished loading yet, so the real response can't arrive
    // in time to race the cancellation
    var exchangeTask = processIO.ExchangeAsync(query, cts.Token);
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exchangeTask).WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_FreshProcess_CompletesOnceReady()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    Task warmUpTask = processIO.WarmUpAsync();
    await warmUpTask.WaitAsync(_timeout);

    Assert.True(warmUpTask.IsCompleted);
  }

  [Fact]
  public async Task WarmUpAsync_AlreadyWarm_CompletesImmediately()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
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
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());
    using var cts = new CancellationTokenSource();

    var warmUpTask = processIO.WarmUpAsync(cts.Token);
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => warmUpTask).WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_ProcessExited_ThrowsInvalidOperationException()
  {
    var processOptions = GetProcessOptions();
    processOptions.Value.ModelPath = "invalidpath";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), processOptions);

    await Assert.ThrowsAsync<InvalidOperationException>(() => processIO.WarmUpAsync()).WaitAsync(_timeout);

    Assert.True(processIO.HasExited);
  }

  [Fact]
  public async Task WarmUpAsync_DisposedWhileWaiting_ThrowsInvalidOperationException()
  {
    var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    var warmUpTask = processIO.WarmUpAsync();
    var disposeTask = processIO.DisposeAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => warmUpTask).WaitAsync(_timeout);
    await disposeTask.AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task WarmUpAsync_CalledOnDisposedInstance_ThrowsObjectDisposedException()
  {
    var processIOOptions = GetProcessIOOptions();
    processIOOptions.Value.ProcessShutdownGracePeriodMs = 1000;
    var processIO = new KataGoProcessIO(processIOOptions, GetProcessOptions());

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    await Assert.ThrowsAsync<ObjectDisposedException>(() => processIO.WarmUpAsync()).WaitAsync(_timeout);
  }

  [Fact]
  public async Task HasLoaded_FreshProcess_ReturnsFalse()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    Assert.False(processIO.HasLoaded);
  }

  [Fact]
  public async Task HasLoaded_AfterWarmUp_ReturnsTrue()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    await processIO.WarmUpAsync().WaitAsync(_timeout);

    Assert.True(processIO.HasLoaded);
  }

  [Fact]
  public async Task HasLoaded_CalledOnDisposedInstance_DoesNotThrow()
  {
    var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    Assert.False(processIO.HasLoaded);
  }

  [Fact]
  public async Task HasExited_FreshProcess_ReturnsFalse()
  {
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    Assert.False(processIO.HasExited);
  }

  [Fact]
  public async Task HasExited_AfterProcessCrashes_ReturnsTrue()
  {
    var processOptions = GetProcessOptions();
    processOptions.Value.ModelPath = "invalidpath";
    await using var processIO = new KataGoProcessIO(GetProcessIOOptions(), processOptions);

    // the process crashes asynchronously; poll rather than assert immediately
    using CancellationTokenSource cts = new(_timeout);
    while (!processIO.HasExited && !cts.IsCancellationRequested)
    {
      await Task.Delay(10);
    }

    Assert.True(processIO.HasExited);
  }

  [Fact]
  public async Task HasExited_CalledOnDisposedInstance_DoesNotThrow()
  {
    var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    Assert.True(processIO.HasExited);
  }

  [Fact]
  public async Task DisposeAsync_AlreadyDisposed_Succeeds()
  {
    var processIO = new KataGoProcessIO(GetProcessIOOptions(), GetProcessOptions());

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task DisposeAsync_ProcessExited_Succeeds()
  {
    var processOptions = GetProcessOptions();
    processOptions.Value.ModelPath = "invalidpath";
    var processIO = new KataGoProcessIO(GetProcessIOOptions(), processOptions);

    // the process crashes asynchronously; poll rather than assert immediately
    using CancellationTokenSource cts = new(_timeout);
    while (!processIO.HasExited && !cts.IsCancellationRequested)
    {
      await Task.Delay(10);
    }

    Assert.True(processIO.HasExited);

    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);
  }

  [Fact]
  public async Task DisposeAsync_AfterRealUse_ExitsGracefully()
  {
    string query = """{"id":"test","moves":[],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true}""";
    var processIOOptions = GetProcessIOOptions();
    processIOOptions.Value.ProcessShutdownGracePeriodMs = 1000;
    var processIO = new KataGoProcessIO(processIOOptions, GetProcessOptions());
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
    var processIOOptions = GetProcessIOOptions();
    processIOOptions.Value.ProcessShutdownGracePeriodMs = 1000;
    var processIO = new KataGoProcessIO(processIOOptions, GetProcessOptions());

    var sw = Stopwatch.StartNew();
    await processIO.DisposeAsync().AsTask().WaitAsync(_timeout);

    Assert.True(
      sw.ElapsedMilliseconds >= 1000,
      $"Expected the forceful-kill path (~1000ms+) that completes under 5000ms, took {sw.ElapsedMilliseconds}ms.");
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
