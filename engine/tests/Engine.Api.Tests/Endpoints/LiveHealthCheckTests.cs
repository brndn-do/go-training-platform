using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Tests.Fakes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Engine.Api.Tests.Endpoints;

public sealed class LiveHealthCheckTests
{
  private static readonly KataGoResponse _response = new("1", null, null, null, null);

  private static readonly HealthCheckContext _healthCheckContext = new();

  [Theory]
  [InlineData(0, false)]
  [InlineData(30000, false)]
  [InlineData(0, true)]
  [InlineData(30000, true)]
  public async Task CheckHealthAsync_ProcessHasNotLoaded_ReturnsUnhealthy(int timeSpentProcessingMs, bool hasExited)
  {
    FakeKataGoClient client = new(_response, false, TimeSpan.FromMilliseconds(timeSpentProcessingMs), hasExited);
    LiveHealthCheck check = new(client, GetOptions());

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Unhealthy(), healthCheckResult);
  }

  [Theory]
  [InlineData(false, 0)]
  [InlineData(false, 30000)]
  [InlineData(true, 0)]
  [InlineData(true, 30000)]
  public async Task CheckHealthAsync_ProcessHasExited_ReturnsUnhealthy(bool hasLoaded, int timeSpentProcessingMs)
  {
    FakeKataGoClient client = new(_response, hasLoaded, TimeSpan.FromMilliseconds(timeSpentProcessingMs), true);
    LiveHealthCheck check = new(client, GetOptions());

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Unhealthy(), healthCheckResult);
  }

  [Fact]
  public async Task CheckHealthAsync_TimeLessThanThreshold_ReturnsHealthy()
  {
    FakeKataGoClient client = new(_response, true, TimeSpan.Zero, false);
    LiveHealthCheck check = new(client, GetOptions());

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Healthy(), healthCheckResult);
  }

  [Theory]
  [InlineData(30000)]
  [InlineData(30001)]
  public async Task CheckHealthAsync_TimeGreaterThanOrEqualToThreshold_ReturnsUnhealthy(int timeSpentProcessingMs)
  {
    FakeKataGoClient client = new(_response, true, TimeSpan.FromMilliseconds(timeSpentProcessingMs), false);
    LiveHealthCheck check = new(client, GetOptions());

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Unhealthy(), healthCheckResult);
  }

  private static IOptions<LiveHealthCheckOptions> GetOptions()
  {
    return Options.Create(new LiveHealthCheckOptions
    {
      ProcessUnresponsiveThresholdMs = 30000,
    });
  }
}