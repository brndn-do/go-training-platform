using System.Text.Json.Serialization;

namespace Engine.Api.Analysis;

/// <summary>
/// A response from KataGo's JSON analysis engine, for either a successful analysis or a
/// rejected query. Deserialize with <c>JsonNamingPolicy.CamelCase</c>, matching KataGo's own
/// field names.
/// </summary>
/// <param name="Id">The id of the query this response answers.</param>
/// <param name="Error">The error message, if the query was rejected; otherwise <c>null</c>.</param>
/// <param name="Policy">
/// The normal net's raw policy prior, computed before any search. Present only when the query set
/// includePolicy. Illegal moves are marked -1.
/// </param>
/// <param name="HumanPolicy">
/// The human SL model's policy. Present only when the query set both includePolicy and
/// humanSLProfile. Illegal moves are marked -1.
/// </param>
/// <param name="RootInfo">Aggregate information about the position, absent on a rejected query.</param>
public sealed record KataGoResponse(
  string Id,
  string? Error,
  double[]? Policy,
  double[]? HumanPolicy,
  KataGoRootInfo? RootInfo)
{
  /// <summary>
  /// Gets a value indicating whether KataGo rejected this query.
  /// </summary>
  [JsonIgnore]
  public bool IsError => Error is not null;
}
