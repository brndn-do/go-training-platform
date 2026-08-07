using Engine.Api.Analysis;

namespace Engine.Api.Tests.Fakes;

public class FakeKataGoClient(KataGoResponse responseToReturn) : IKataGoClient
{
  public KataGoQuery? QueryReceived { get; private set; }

  public Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default)
  {
    QueryReceived = query;
    return Task.FromResult(responseToReturn);
  }
}