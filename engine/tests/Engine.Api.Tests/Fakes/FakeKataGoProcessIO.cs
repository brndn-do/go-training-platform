using Engine.Api.Processes;

namespace Engine.Api.Tests.Fakes;

public sealed class FakeKataGoProcessIO(TaskCompletionSource<string?>[] responses, TaskCompletionSource? processReadyTcs = null, bool hasExited = false) : IKataGoProcessIO
{
  private readonly TaskCompletionSource _processReadyTcs = processReadyTcs ?? new();

  private int _exchangeCallCount;

  public string?[] RequestsReceived { get; private set; } = [];

  public int WarmUpCallCount { get; private set; }

  public bool HasLoaded => _processReadyTcs.Task.IsCompletedSuccessfully;

  public bool HasExited { get; } = hasExited;

  public async Task<string?> ExchangeAsync(string request, CancellationToken cancellationToken = default)
  {
    RequestsReceived = [.. RequestsReceived.Append(request)];
    return await responses[_exchangeCallCount++].Task.WaitAsync(cancellationToken);
  }

  public async Task WarmUpAsync(CancellationToken cancellationToken = default)
  {
    WarmUpCallCount++;
    await _processReadyTcs.Task.WaitAsync(cancellationToken);
  }
}
