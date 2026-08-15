using Engine.Api.Analysis;

namespace Engine.Api.Tests.Fakes;

public sealed class FakeKataGoClient(KataGoResponse responseToReturn, bool hasLoaded = true, bool isResponsive = true) : IKataGoClient
{
  public KataGoQuery? QueryReceived { get; private set; }

  public bool HasLoaded { get; } = hasLoaded;

  public bool IsResponsive { get; } = isResponsive;

  public Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default)
  {
    QueryReceived = query;
    return Task.FromResult(responseToReturn);
  }

  public Task WarmUpAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}