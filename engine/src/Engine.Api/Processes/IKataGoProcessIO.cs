namespace Engine.Api.Processes;

/// <summary>
/// Sends a single line to KataGo's analysis engine process and reads back its response line.
/// </summary>
public interface IKataGoProcessIO
{
  /// <summary>
  /// Gets a value indicating whether the KataGo process has finished loading and is ready to
  /// serve queries. A non-blocking snapshot check, unlike <see cref="WarmUpAsync"/> — never
  /// throws, including after disposal.
  /// </summary>
  bool HasLoaded { get; }

  /// <summary>
  /// Gets a value indicating whether the KataGo process has exited. A non-blocking snapshot
  /// check, unlike <see cref="WarmUpAsync"/> — never throws, including after disposal.
  /// </summary>
  bool HasExited { get; }

  /// <summary>
  /// Writes <paramref name="request"/> as a line to the process, then reads and returns the
  /// next line written back.
  /// </summary>
  /// <param name="request">The line to send.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The line read back, or <c>null</c> if the stream ended before one arrived.</returns>
  Task<string?> ExchangeAsync(string request, CancellationToken cancellationToken = default);

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
