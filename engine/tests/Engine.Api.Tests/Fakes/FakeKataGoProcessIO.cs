using Engine.Api.Processes;

namespace Engine.Api.Tests.Fakes;

public class FakeKataGoProcessIO(string?[] responses, TaskCompletionSource<string?>[] gates) : IKataGoProcessIO
{
  private int _callCount;

  public string?[] RequestsReceived { get; private set; } = [];

  public async Task<string?> ExchangeAsync(string request, CancellationToken cancellationToken = default)
  {
    RequestsReceived = [.. RequestsReceived.Append(request)];
    await gates[_callCount].Task.WaitAsync(cancellationToken);
    return responses[_callCount++];
  }
}
