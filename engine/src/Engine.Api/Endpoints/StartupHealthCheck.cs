using Engine.Api.Analysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Engine.Api.Endpoints;

/// <summary>
/// Reports healthy once the KataGo process has finished loading.
/// </summary>
public sealed class StartupHealthCheck(IKataGoClient client) : IHealthCheck
{
  /// <inheritdoc/>
  public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    if (client.HasLoaded)
    {
      return Task.FromResult(HealthCheckResult.Healthy());
    }

    return Task.FromResult(HealthCheckResult.Unhealthy());
  }
}