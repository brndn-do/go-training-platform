namespace Engine.Api.Analysis;

/// <summary>
/// A suggested move and win rate for a given position.
/// </summary>
/// <param name="Move">The suggested move, or <c>null</c> for a pass.</param>
/// <param name="Winrate">The resulting win rate.</param>
public sealed record SuggestionResponse(Move? Move, double Winrate);
