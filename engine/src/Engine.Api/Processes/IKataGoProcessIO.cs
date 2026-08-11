namespace Engine.Api.Processes;

/// <summary>
/// Sends a single line to KataGo's analysis engine process and reads back its response line.
/// </summary>
public interface IKataGoProcessIO
{
  /// <summary>
  /// Writes <paramref name="request"/> as a line to the process, then reads and returns the
  /// next line written back.
  /// </summary>
  /// <param name="request">The line to send.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The line read back, or <c>null</c> if the stream ended before one arrived.</returns>
  Task<string?> ExchangeAsync(string request, CancellationToken cancellationToken = default);
}
