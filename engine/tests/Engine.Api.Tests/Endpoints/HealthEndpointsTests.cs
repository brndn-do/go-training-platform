using System.Net;
using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Api.Tests.Endpoints;

public sealed class HealthEndpointsTests
{
  private static readonly KataGoResponse _response = new("1", null, null, null, null);

  [Fact]
  public async Task HealthStartup_ProcessHasLoaded_ReturnsOk()
  {
    using var factory = Factory(hasLoaded: true);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/startup");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task HealthStartup_ProcessHasNotLoaded_ReturnsServiceUnavailable()
  {
    using var factory = Factory(hasLoaded: false);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/startup");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthReady_ProcessHasNotLoaded_ReturnsServiceUnavailable()
  {
    using var factory = Factory(hasLoaded: false);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/ready");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthReady_ProcessHasExited_ReturnsServiceUnavailable()
  {
    using var factory = Factory(hasExited: true);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/ready");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthReady_TimeLessThanThreshold_ReturnsOk()
  {
    using var factory = Factory(timeSpentProcessing: TimeSpan.FromMilliseconds(0));
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/ready");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task HealthReady_TimeGreaterThanThreshold_ReturnsServiceUnavailable()
  {
    using var factory = Factory(timeSpentProcessing: TimeSpan.FromMilliseconds(10001));
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/ready");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthLive_ProcessHasNotLoaded_ReturnsServiceUnavailable()
  {
    using var factory = Factory(hasLoaded: false);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthLive_ProcessHasExited_ReturnsServiceUnavailable()
  {
    using var factory = Factory(hasExited: true);
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  [Fact]
  public async Task HealthLive_TimeLessThanThreshold_ReturnsOk()
  {
    using var factory = Factory(timeSpentProcessing: TimeSpan.FromMilliseconds(0));
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task HealthLive_TimeGreaterThanThreshold_ReturnsServiceUnavailable()
  {
    using var factory = Factory(timeSpentProcessing: TimeSpan.FromMilliseconds(30001));
    using var client = factory.CreateClient();

    var response = await client.GetAsync("/health/live");

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
  }

  // Creates a new WebApplicationFactory<Program> that overrides DI registrations with a FakeKataGoClient.
  // The FakeKataGoClient sets the health properties (.HasLoaded, .TimeSpentProcessing, .HasExited) to
  // whatever you pass in as arguments to this factory method.
  private static WebApplicationFactory<Program> Factory(bool hasLoaded = true, TimeSpan timeSpentProcessing = default, bool hasExited = false) =>
    new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IKataGoClient>();
        services.AddSingleton<IKataGoClient>(new FakeKataGoClient(_response, hasLoaded, timeSpentProcessing, hasExited));
        services.Configure<ReadyHealthCheckOptions>(o =>
        {
          o.ProcessUnresponsiveThresholdMs = 10000;
        });
        services.Configure<LiveHealthCheckOptions>(o =>
        {
          o.ProcessUnresponsiveThresholdMs = 30000;
        });
      }));
}