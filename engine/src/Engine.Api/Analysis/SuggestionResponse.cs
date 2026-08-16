namespace Engine.Api.Analysis;

/// <summary>
/// A suggested move and win rate for a given position.
/// </summary>
/// <param name="Move">The suggested move, or <c>null</c> for a pass.</param>
/// <param name="BlackWinRate">
/// The resulting estimate of Black's win probability, regardless of whose turn it is.
/// </param>
public sealed record SuggestionResponse(Move? Move, double BlackWinRate);
