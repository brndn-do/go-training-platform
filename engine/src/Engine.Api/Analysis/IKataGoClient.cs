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
}