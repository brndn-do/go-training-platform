namespace Engine.Api.Analysis;

/// <summary>
/// Aggregate information about the position a <see cref="KataGoResponse"/> was computed for.
/// </summary>
/// <param name="Winrate">The normal net's estimated win probability, per reportAnalysisWinratesAs.</param>
/// <param name="HumanWinrate">
/// The human SL model's estimated win probability. Present only when the query set humanSLProfile.
/// </param>
public sealed record KataGoRootInfo(double Winrate, double? HumanWinrate);
