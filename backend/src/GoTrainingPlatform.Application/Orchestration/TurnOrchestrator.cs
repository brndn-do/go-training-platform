using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;

namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Sequences human and bot turns: takes a human action, and — unlike <see cref="GameService"/>,
/// decides whether the bot should respond next, asking <see cref="IEngineClient"/> for its
/// move when it should.
/// </summary>
public sealed class TurnOrchestrator(GameService gameService, IEngineClient engineClient)
{
  /// <summary>
  /// Starts a new game for a human player.
  /// </summary>
  /// <param name="playerColor">The color the human player is playing as.</param>
  /// <param name="boardSize">The width and height of the (square) board.</param>
  /// <param name="botStrength">The strength of the bot for this game.</param>
  /// <param name="komi">The komi to use for this game.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The newly created game, and a hint for the human's next move.</returns>
  /// <exception cref="InvalidBotResponseException">
  /// If the bot response from the engine is an invalid operation.
  /// </exception>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// If <paramref name="boardSize"/> is not positive.
  /// </exception>
  public async Task<OrchestrationResult> StartGameAsync(
    Color playerColor,
    int boardSize,
    BotStrength botStrength,
    double komi = 7.5,
    CancellationToken cancellationToken = default)
  {
    Game game = await gameService.StartGameAsync(playerColor, boardSize, botStrength, komi, cancellationToken);

    if (game.Turn != playerColor)
    {
      game = await MakeBotPlayAsync(game, cancellationToken);
    }

    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  /// <summary>
  /// Loads a game, advancing the bot's turn first if it was already the bot's turn —
  /// covering a bot response that was never recorded (e.g. after a crash between the
  /// human's move and the bot's response) alongside the ordinary case.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the loaded
  /// game and a hint for the human's next move.
  /// </returns>
  /// <exception cref="InvalidBotResponseException">
  /// If the bot response from the engine is an invalid operation.
  /// </exception>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  public async Task<OrchestrationResult> LoadGameAsync(Guid gameId, CancellationToken cancellationToken = default)
  {
    Game? game = await gameService.LoadGameAsync(gameId, cancellationToken);
    if (game is null)
    {
      return new OrchestrationResult(null, false, null);
    }

    if (game.Turn != game.PlayerColor)
    {
      game = await MakeBotPlayAsync(game, cancellationToken);
    }

    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  /// <summary>
  /// Attempts to record a stone placement for the human player, advancing the
  /// bot's turn next if the move was accepted.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="x">The X coordinate of the move, zero-indexed.</param>
  /// <param name="y">The Y coordinate of the move, zero-indexed.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the game, with
  /// <c>Success</c> reflecting whether the move was accepted, and a hint for the human's next
  /// move if it was.
  /// </returns>
  /// <exception cref="InvalidBotResponseException">
  /// If the bot response from the engine is an invalid operation.
  /// </exception>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  public async Task<OrchestrationResult> MakeMoveAsync(
    Guid gameId,
    int x,
    int y,
    CancellationToken cancellationToken = default)
  {
    GameActionResult actionResult = await gameService.MakeMoveAsync(gameId, Actor.Human, x, y, cancellationToken);
    if (!actionResult.Success)
    {
      return new OrchestrationResult(actionResult.Game, false, null);
    }

    Game game = actionResult.Game!;

    if (game.Turn != game.PlayerColor)
    {
      game = await MakeBotPlayAsync(game, cancellationToken);
    }

    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  /// <summary>
  /// Attempts to record a pass for the human player, advancing the bot's turn
  /// next if the pass was accepted.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the game, with
  /// <c>Success</c> reflecting whether the pass was accepted, and a hint for the human's next
  /// move if it was.
  /// </returns>
  /// <exception cref="InvalidBotResponseException">
  /// If the bot response from the engine is an invalid operation.
  /// </exception>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  public async Task<OrchestrationResult> MakePassAsync(
    Guid gameId,
    CancellationToken cancellationToken = default)
  {
    GameActionResult actionResult = await gameService.MakePassAsync(gameId, Actor.Human, cancellationToken);
    if (!actionResult.Success)
    {
      return new OrchestrationResult(actionResult.Game, false, null);
    }

    Game game = actionResult.Game!;

    if (game.Turn != game.PlayerColor)
    {
      game = await MakeBotPlayAsync(game, cancellationToken);
    }

    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  /// <summary>
  /// Attempts to undo back to before the human player's most recent move. Never advances the
  /// bot's turn — a successful undo always lands back on the human's turn.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the game, with
  /// <c>Success</c> reflecting whether the undo was applied, and a hint for the human's next
  /// move if it was.
  /// </returns>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  public async Task<OrchestrationResult> UndoAsync(Guid gameId, CancellationToken cancellationToken = default)
  {
    GameActionResult actionResult = await gameService.UndoAsync(gameId, cancellationToken);
    if (!actionResult.Success)
    {
      return new OrchestrationResult(actionResult.Game, false, null);
    }

    Game game = actionResult.Game!;
    var suggestionForHuman = await GetSuggestionForHumanAsync(game, cancellationToken);

    return new OrchestrationResult(game, true, suggestionForHuman);
  }

  /// <summary>
  /// Attempts to resign the game for the human player. Never advances the
  /// bot's turn — resigning ends the game immediately.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>
  /// A result with <c>Game: null</c> if no game with that id exists; otherwise the game, with
  /// <c>Success</c> reflecting whether the resignation was accepted, and a final win-rate
  /// estimate if it was.
  /// </returns>
  /// <exception cref="EngineException">
  /// If the engine cannot be reached, rejects the request, or answers with something that
  /// cannot be read.
  /// </exception>
  /// <exception cref="RepositoryException">
  /// If the store cannot be reached (<see cref="RepositoryFailureKind.Unavailable"/>), refuses
  /// the operation (<see cref="RepositoryFailureKind.Rejected"/>), or the game was changed by
  /// someone else since it was loaded (<see cref="RepositoryFailureKind.Conflict"/>).
  /// </exception>
  public async Task<OrchestrationResult> ResignAsync(
    Guid gameId,
    CancellationToken cancellationToken = default)
  {
    GameActionResult actionResult = await gameService.ResignAsync(gameId, Actor.Human, cancellationToken);
    if (!actionResult.Success)
    {
      return new OrchestrationResult(actionResult.Game, false, null);
    }

    Game game = actionResult.Game!;
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
      ? gameService.MakePassAsync(game.Id, Actor.Bot, cancellationToken)
      : gameService.MakeMoveAsync(game.Id, Actor.Bot, coordinates.X, coordinates.Y, cancellationToken));

    if (!result.Success)
    {
      throw new InvalidBotResponseException();
    }

    return result.Game!;
  }
}