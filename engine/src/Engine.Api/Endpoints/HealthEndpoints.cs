namespace Engine.Api.Endpoints;

/// <summary>
/// Maps the health check endpoint.
/// </summary>
public static class HealthEndpoints
{
  /// <summary>
  /// Maps a health check endpoint at <c>/health</c>.
  /// </summary>
  /// <param name="app">The endpoint route builder to map the endpoint on.</param>
  /// <returns>The same <paramref name="app"/>, for chaining.</returns>
  public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapHealthChecks("/health");
    return app;
  }
}
