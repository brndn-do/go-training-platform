namespace Engine.Api.Analysis;

/// <summary>
/// Queries KataGo's JSON analysis engine.
/// </summary>
public interface IKataGoClient
{
  /// <summary>
  /// Queries the KataGo process for analysis.
  /// </summary>
  /// <param name="query">The query to analyze.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The KataGo response.</returns>
  Task<KataGoResponse> QueryAsync(KataGoQuery query, CancellationToken cancellationToken = default);

  /// <summary>
  /// Waits until the KataGo process has finished loading and is ready to serve queries. Safe
  /// to call multiple times or concurrently from multiple callers.
  /// </summary>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A task that completes once the process is ready.</returns>
  /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
  /// <exception cref="InvalidOperationException">The process exited before becoming ready.</exception>
  /// <exception cref="ObjectDisposedException">Called after this instance was disposed.</exception>
  Task WarmUpAsync(CancellationToken cancellationToken = default);
}