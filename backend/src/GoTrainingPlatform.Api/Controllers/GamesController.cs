using GoTrainingPlatform.Api.Contracts;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.Controllers;

/// <summary>
/// The endpoints for playing a game. Every action is taken as the current player, and every
/// response carries the full board, since the client has no rules engine to derive it.
/// </summary>
[ApiController]
[Route("api/games")]
public sealed class GamesController(TurnOrchestrator orchestrator, GameService gameService) : ControllerBase
{
  /// <summary>
  /// Starts a new game, playing the bot's opening move first if the bot has the first turn.
  /// </summary>
  /// <param name="request">The game's settings.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The new game, and a hint for the player's first move.</returns>
  [HttpPost]
  [ProducesResponseType(StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<GameResponse>> Start(StartGameRequest request, CancellationToken cancellationToken)
  {
    var result = await orchestrator.StartGameAsync(
      request.PlayerColor!.Value,
      request.BoardSize!.Value,
      request.BotStrength!.Value,
      request.Komi,
      cancellationToken);

    var gameResponse = GameResponse.From(result.Game!, result.Suggestion);

    return CreatedAtAction(nameof(Get), new { gameId = gameResponse.Id }, gameResponse);
  }

  /// <summary>
  /// Reads a game without changing it. Unlike <see cref="Resume"/> this never advances the
  /// bot's turn and never asks the engine for a hint.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game, with no suggestion.</returns>
  [HttpGet("{gameId}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Get(Guid gameId, CancellationToken cancellationToken)
  {
    var game = await gameService.LoadGameAsync(gameId, cancellationToken);

    if (game is null)
    {
      return NotFound();
    }

    var gameResponse = GameResponse.From(game, null);

    return Ok(gameResponse);
  }

  /// <summary>
  /// Reads a game, playing the bot's move first if it is the bot's turn — covering a bot
  /// response that was never recorded, alongside the ordinary case.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game, and a hint for the player's next move.</returns>
  [HttpPost("{gameId}/resume")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Resume(Guid gameId, CancellationToken cancellationToken)
  {
    var result = await orchestrator.LoadGameAsync(gameId, cancellationToken);

    return ToResponse(result);
  }

  /// <summary>
  /// Plays a stone for the player, then plays the bot's reply.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="request">The point to play.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game after both moves, and a hint for the player's next move.</returns>
  [HttpPost("{gameId}/moves")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Move(Guid gameId, MoveRequest request, CancellationToken cancellationToken)
  {
    var result = await orchestrator.MakeMoveAsync(gameId, request.X!.Value, request.Y!.Value, cancellationToken);

    return ToResponse(result);
  }

  /// <summary>
  /// Passes for the player, then plays the bot's reply. Two consecutive passes end the game.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game after both turns, and a hint for the player's next move.</returns>
  [HttpPost("{gameId}/pass")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Pass(Guid gameId, CancellationToken cancellationToken)
  {
    var result = await orchestrator.MakePassAsync(gameId, cancellationToken);

    return ToResponse(result);
  }

  /// <summary>
  /// Retracts the player's last move along with the bot's reply to it.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The game as it stood before the move, and a hint for replaying it.</returns>
  [HttpPost("{gameId}/undo")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Undo(Guid gameId, CancellationToken cancellationToken)
  {
    var result = await orchestrator.UndoAsync(gameId, cancellationToken);

    return ToResponse(result);
  }

  /// <summary>
  /// Resigns the game for the player, ending it immediately.
  /// </summary>
  /// <param name="gameId">The game's id.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The finished game, and a final win-rate estimate.</returns>
  [HttpPost("{gameId}/resign")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<GameResponse>> Resign(Guid gameId, CancellationToken cancellationToken)
  {
    var result = await orchestrator.ResignAsync(gameId, cancellationToken);

    return ToResponse(result);
  }

  // Maps an orchestration outcome onto a response: a missing game is not found, which also
  // covers a game the current player does not own, and a rejected action is a bad request
  // carrying no reason.
  private ActionResult<GameResponse> ToResponse(OrchestrationResult result)
  {
    if (result.Game is null)
    {
      return NotFound();
    }

    if (!result.Success)
    {
      return BadRequest();
    }

    return Ok(GameResponse.From(result.Game, result.Suggestion));
  }
}
