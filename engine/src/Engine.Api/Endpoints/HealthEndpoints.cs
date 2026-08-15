using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Engine.Api.Endpoints;

/// <summary>
/// Maps the health check endpoints.
/// </summary>
public static class HealthEndpoints
{
  /// <summary>
  /// Maps the startup, readiness, and liveness health check endpoints at
  /// <c>GET /health/startup</c>, <c>GET /health/ready</c>, and <c>GET /health/live</c>.
  /// </summary>
  /// <param name="app">The endpoint route builder to map the endpoints on.</param>
  /// <returns>The same <paramref name="app"/>, for chaining.</returns>
  public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapHealthChecks(
      "/health/startup",
      new HealthCheckOptions { Predicate = c => c.Tags.Contains("startup") });

    app.MapHealthChecks(
      "/health/ready",
      new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

    app.MapHealthChecks(
      "/health/live",
      new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });

    return app;
  }
}
