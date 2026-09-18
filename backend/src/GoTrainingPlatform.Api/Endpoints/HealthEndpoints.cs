using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace GoTrainingPlatform.Api.Endpoints;

/// <summary>
/// Maps the health check endpoints.
/// </summary>
public static class HealthEndpoints
{
  /// <summary>
  /// Maps the startup, readiness, and liveness health check endpoints at
  /// <c>GET /health/startup</c>, <c>GET /health/ready</c>, and <c>GET /health/live</c>.
  /// Each reports healthy whenever the host is able to answer; none runs a check.
  /// </summary>
  /// <param name="app">The endpoint route builder to map the endpoints on.</param>
  /// <returns>The same <paramref name="app"/>, for chaining.</returns>
  public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
  {
    // No condition is evaluated: the signal is whether the host answers at all.
    var runNoChecks = new HealthCheckOptions { Predicate = _ => false };

    app.MapHealthChecks("/health/startup", runNoChecks);
    app.MapHealthChecks("/health/ready", runNoChecks);
    app.MapHealthChecks("/health/live", runNoChecks);

    return app;
  }
}
