using System.Text.Json.Serialization;

namespace Engine.Api.Analysis;

/// <summary>
/// Aggregate information about the position a <see cref="KataGoResponse"/> was computed for.
/// </summary>
/// <param name="BlackWinRate">
/// The normal net's estimate of Black's win probability. Black's regardless of whose turn it is,
/// per the config's <c>reportAnalysisWinratesAs = BLACK</c>.
/// </param>
/// <param name="HumanBlackWinRate">
/// The human SL model's estimate of Black's win probability. Present only when the query set
/// humanSLProfile.
/// </param>
public sealed record KataGoRootInfo(
  [property: JsonPropertyName("winrate")] double BlackWinRate,
  [property: JsonPropertyName("humanWinrate")] double? HumanBlackWinRate);
