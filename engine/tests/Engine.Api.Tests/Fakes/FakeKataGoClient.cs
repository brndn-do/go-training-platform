using Engine.Api.Analysis;

namespace Engine.Api.Tests.Fakes;

public sealed class FakeKataGoClient(KataGoResponse responseToReturn, bool isReady = true) : IKataGoClient
{
  public KataGoQuery? QueryReceived { get; private set; }

  public bool IsReady { get; } = isReady;

  public Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default)
  {
    QueryReceived = query;
    return Task.FromResult(responseToReturn);
  }

  public Task WarmUpAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}