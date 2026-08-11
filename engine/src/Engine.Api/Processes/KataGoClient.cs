using System.Text.Json;
using System.Threading.Channels;
using Engine.Api.Analysis;

namespace Engine.Api.Processes;

/// <summary>
/// Queries KataGo's JSON analysis engine over a single, persistent process, serializing
/// concurrent callers through a background worker so only one query is ever in flight
/// against the process at a time.
/// </summary>
public sealed class KataGoClient : IKataGoClient, IAsyncDisposable
{
  private static readonly JsonSerializerOptions _options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private readonly TimeSpan _shutdownGracePeriod;

  private readonly IKataGoProcessIO _processIO;

  // async-friendly, thread-safe queue with 1 reader (the worker loop) at a time
  // and multiple writers (callers) writing concurrently
  private readonly Channel<WorkItem> _channel = Channel.CreateUnbounded<WorkItem>(
    new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

  private readonly CancellationTokenSource _shutdownCts = new();
  private readonly Task _workerTask;

  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoClient"/> class, starting its
  /// background worker immediately.
  /// </summary>
  /// <param name="processIO">The process I/O to exchange queries and responses through.</param>
  /// <param name="shutdownGracePeriodMs">
  /// How long <see cref="DisposeAsync"/> waits for in-flight work to finish on its own before
  /// forcefully cancelling it.
  /// </param>
  public KataGoClient(IKataGoProcessIO processIO, int shutdownGracePeriodMs = 5000)
  {
    _shutdownGracePeriod = TimeSpan.FromMilliseconds(shutdownGracePeriodMs);
    _processIO = processIO;
    _workerTask = Task.Run(() => ProcessQueueAsync(_shutdownCts.Token));
  }

  /// <summary>
  /// Stops accepting new queries and shuts down the background worker: queued and in-flight
  /// work is given up to the configured grace period to finish naturally, after which any
  /// still-pending work is cancelled so this never hangs indefinitely.
  /// </summary>
  /// <returns>A task that represents the asynchronous dispose operation.</returns>
  public async ValueTask DisposeAsync()
  {
    _channel.Writer.TryComplete();

    // Let currently-queued/in-flight work finish naturally; only cancel if it doesn't
    // finish within the grace period.
    Task gracefulDrain = await Task.WhenAny(_workerTask, Task.Delay(_shutdownGracePeriod));
    if (gracefulDrain != _workerTask)
    {
      _shutdownCts.Cancel();
    }

    try
    {
      await _workerTask;
    }
    catch (OperationCanceledException)
    {
    }

    // Fail anything left so callers don't hang
    while (_channel.Reader.TryRead(out var leftover))
    {
      leftover.CompletionSource.TrySetCanceled();
    }

    _shutdownCts.Dispose();
  }

  /// <inheritdoc/>
  public async Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default)
  {
    var item = new WorkItem(query);

    // register a cancellation callback: if caller's token is cancelled,
    // call TrySetCancelled() on the TCS
    await using var registration = cancellationToken.Register(
      static state => ((TaskCompletionSource<KataGoResponse>)state!).TrySetCanceled(),
      item.CompletionSource);

    await _channel.Writer.WriteAsync(item, cancellationToken); // queue the item

    return await item.CompletionSource.Task; // wait for the background worker loop to resolve
  }

  private async Task ProcessQueueAsync(CancellationToken shutdownToken)
  {
    await foreach (WorkItem item in _channel.Reader.ReadAllAsync(shutdownToken))
    {
      if (item.CompletionSource.Task.IsCompleted)
      {
        continue; // already cancelled
      }

      try
      {
        KataGoResponse response = await ProcessQuery(item.Query, shutdownToken);
        item.CompletionSource.TrySetResult(response);
      }
      catch (Exception ex)
      {
        item.CompletionSource.TrySetException(ex);
      }
    }
  }

  private async Task<KataGoResponse> ProcessQuery(KataGoQuery query, CancellationToken cancellationToken)
  {
    string json = JsonSerializer.Serialize(query, _options);
    string? line = await _processIO.ExchangeAsync(json, cancellationToken);

    KataGoResponse response = line is null
      ? new KataGoResponse(query.Id, "KataGo ProcessIO did not respond.", null, null, null)
      : JsonSerializer.Deserialize<KataGoResponse>(line, _options)
        ?? new KataGoResponse(query.Id, "KataGo process returned null.", null, null, null);

    return response;
  }

  // Bundles the request (Query) with a TaskCompletionSource<KataGoResponse> (TCS).
  // Caller (QueryAsync) awaits the TCS, while the background worker loop resolves it once it has the response.
  private sealed class WorkItem(KataGoQuery query)
  {
    public KataGoQuery Query { get; } = query;

    // RunContinuationsAsynchronously: prevents completing this task from blocking the worker loop
    // with the caller's continuation code.
    public TaskCompletionSource<KataGoResponse> CompletionSource { get; } =
      new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
