using Engine.Api.Analysis;

namespace Engine.Api.Endpoints;

/// <summary>
/// Maps the warm-up endpoint.
/// </summary>
public static class WarmUpEndpoints
{
  /// <summary>
  /// Maps the warm-up endpoint at <c>POST /warmup</c>.
  /// </summary>
  /// <param name="app">The endpoint route builder to map the endpoint on.</param>
  /// <returns>The same <paramref name="app"/>, for chaining.</returns>
  public static IEndpointRouteBuilder MapWarmUpEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapPost(
      "/warmup",
      async (IKataGoClient client, CancellationToken cancellationToken) =>
      {
        try
        {
          await client.WarmUpAsync(cancellationToken);
          return Results.Ok();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
          return Results.Problem(
            title: "An error occurred while warming up the service.",
            statusCode: StatusCodes.Status500InternalServerError);
        }
      });

    return app;
  }
}