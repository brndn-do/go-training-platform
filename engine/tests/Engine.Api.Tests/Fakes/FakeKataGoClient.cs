using Engine.Api.Analysis;

namespace Engine.Api.Tests.Fakes;

public sealed class FakeKataGoClient(
  KataGoResponse responseToReturn,
  bool hasLoaded = true,
  TimeSpan timeSpentProcessing = default,
  bool hasExited = false,
  Exception? warmUpException = null) : IKataGoClient
{
  public KataGoQuery? QueryReceived { get; private set; }

  public bool HasLoaded { get; } = hasLoaded;

  public TimeSpan TimeSpentProcessing { get; } = timeSpentProcessing;

  public bool HasExited { get; } = hasExited;

  public Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default)
  {
    QueryReceived = query;
    return Task.FromResult(responseToReturn);
  }

  public Task WarmUpAsync(CancellationToken cancellationToken = default) =>
    warmUpException is null ? Task.CompletedTask : Task.FromException(warmUpException);
}