using GoTrainingPlatform.Api.Contracts;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(TurnOrchestrator orchestrator, GameService gameService) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Start(StartGameRequest request, CancellationToken cancellationToken = default)
  {
    var result = await orchestrator.StartGameAsync(
      request.PlayerColor!.Value,
      request.BoardSize!.Value,
      request.BotStrength!.Value,
      request.Komi,
      cancellationToken);

    var gameResponse = GameResponse.From(result.Game!, result.Suggestion);

    return CreatedAtAction(nameof(Get), new { id = result.Game!.Id }, gameResponse);
  }

  [HttpGet("{gameId}")]
  public async Task<IActionResult> Get(Guid gameId, CancellationToken cancellationToken = default)
  {
    var game = await gameService.LoadGameAsync(gameId, cancellationToken);

    if (game is null)
    {
      return NotFound();
    }

    var gameResponse = GameResponse.From(game, null);

    return Ok(gameResponse);
  }
}