using Engine.Api.Analysis;

namespace Engine.Api.Endpoints;

/// <summary>
/// Maps the suggestion endpoint.
/// </summary>
public static class SuggestionEndpoints
{
  /// <summary>
  /// Maps the suggestion endpoint at <c>POST /suggestion</c>.
  /// </summary>
  /// <param name="app">The endpoint route builder to map the endpoint on.</param>
  /// <returns>The same <paramref name="app"/>, for chaining.</returns>
  public static IEndpointRouteBuilder MapSuggestionEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapPost(
      "/suggestion",
      async (SuggestionRequest request, SuggestionService suggestionService, CancellationToken cancellationToken) =>
      {
        try
        {
          SuggestionResponse suggestion = await suggestionService.GetSuggestionAsync(
            request.Moves, request.BoardSize, request.Komi, request.BotStrength, cancellationToken);
          return Results.Ok(suggestion);
        }
        catch (InvalidKataGoResponseException)
        {
          return Results.Problem(
            title: "An error occurred while generating a suggestion.",
            statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (ArgumentException ex)
        {
          return Results.Problem(
            title: "Invalid suggestion request",
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);
        }
      });

    return app;
  }
}
