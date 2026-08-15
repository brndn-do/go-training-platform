using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Tests.Fakes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Engine.Api.Tests.Endpoints;

public sealed class StartupHealthCheckTests
{
  private static readonly KataGoResponse _response = new("1", null, null, null, null);

  private static readonly HealthCheckContext _healthCheckContext = new();

  // should ONLY depend on the HasLoaded flag, regardless of the other two flags
  [Theory]
  [InlineData(0, true)]
  [InlineData(0, false)]
  [InlineData(100000, false)]
  [InlineData(100000, true)]
  public async Task CheckHealthAsync_ProcessHasLoaded_ReturnsHealthy(int timeSpentProcessingMs, bool hasExited)
  {
    FakeKataGoClient client = new(_response, true, TimeSpan.FromMilliseconds(timeSpentProcessingMs), hasExited);
    StartupHealthCheck check = new(client);

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Healthy(), healthCheckResult);
  }

  // should ONLY depend on the HasLoaded flag, regardless of the other two flags
  [Theory]
  [InlineData(0, true)]
  [InlineData(0, false)]
  [InlineData(100000, false)]
  [InlineData(100000, true)]
  public async Task CheckHealthAsync_ProcessHasNotLoaded_ReturnsUnhealthy(int timeSpentProcessingMs, bool hasExited)
  {
    FakeKataGoClient client = new(_response, false, TimeSpan.FromMilliseconds(timeSpentProcessingMs), hasExited);
    StartupHealthCheck check = new(client);

    var healthCheckResult = await check.CheckHealthAsync(_healthCheckContext);

    Assert.Equal(HealthCheckResult.Unhealthy(), healthCheckResult);
  }
}