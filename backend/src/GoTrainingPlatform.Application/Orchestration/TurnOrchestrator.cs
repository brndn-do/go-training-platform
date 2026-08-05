using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Sequences human and bot turns: takes a human action, and — unlike <see cref="GameService"/>,
/// decides whether the bot should respond next, asking <see cref="IEngineClient"/> for its
/// move when it should.
/// </summary>
public class TurnOrchestrator(GameService gameService, IEngineClient engineClient)
{
  /// <summary>
  /// Starts a new game for a human player.
  /// </summary>
  /// <param name="playerId">The id of the human player.</param>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="botStrength">The strength of the bot for this game.</param>
  /// <param name="komi">The komi to use for this game.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The newly created game, and a hint for the human's next move.</returns>
  /// <exception cref="InvalidBotResponseException">
  /// If the bot response from the engine is an invalid operation.
  /// </exception>
  public async Task<OrchestrationResult> StartGameAsync(
    Guid playerId,
    Color playerColor,
    int boardSize,
    BotStrength botStrength,
    double komi = 7.5,
    CancellationToken cancellationToken = default)
  {
    Game game = await gameService.StartGameAsync(playerId, playerColor, boardSize, botStrength, komi, cancellationToken);

    if (game.Turn != playerColor)
    {
      game = await MakeBotPlayAsync(game, cancellationToken);
    }

    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  private async Task<EngineSuggestion> GetSuggestionForHumanAsync(Game game, CancellationToken cancellationToken) =>
    await engineClient.GetSuggestionAsync(
      game.Moves,
      game.BoardSize,
      game.Komi,
      BotStrength.Superhuman, // always Superhuman for player hints
      cancellationToken);

  private async Task<Game> MakeBotPlayAsync(Game game, CancellationToken cancellationToken)
  {
    var suggestionForBot = await engineClient.GetSuggestionAsync(
      game.Moves,
      game.BoardSize,
      game.Komi,
      game.BotStrength,
      cancellationToken);

    var coordinates = suggestionForBot.Coordinates;

    GameActionResult result = await (
      coordinates is null
      ? gameService.MakePassAsync(game.Id, game.Turn, cancellationToken)
      : gameService.MakeMoveAsync(game.Id, game.Turn, coordinates.X, coordinates.Y, cancellationToken));

    if (!result.Success)
    {
      throw new InvalidBotResponseException();
    }

    return result.Game!;
  }
}