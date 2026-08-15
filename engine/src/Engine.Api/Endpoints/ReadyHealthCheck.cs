using Engine.Api.Analysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Engine.Api.Endpoints;

/// <summary>
/// Reports healthy while KataGo is loaded, hasn't exited, and hasn't been processing a
/// single query longer than the configured threshold.
/// </summary>
public sealed class ReadyHealthCheck(IKataGoClient client, IOptions<ReadyHealthCheckOptions> options) : IHealthCheck
{
  /// <inheritdoc/>
  public Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    TimeSpan stuckThreshold = TimeSpan.FromMilliseconds(options.Value.ProcessUnresponsiveThresholdMs);

    if (client.HasLoaded && !client.HasExited && client.TimeSpentProcessing < stuckThreshold)
    {
      return Task.FromResult(HealthCheckResult.Healthy());
    }

    return Task.FromResult(HealthCheckResult.Unhealthy());
  }
}