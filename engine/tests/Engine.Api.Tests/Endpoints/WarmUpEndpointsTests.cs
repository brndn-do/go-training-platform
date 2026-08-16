using System.Net;
using Engine.Api.Analysis;
using Engine.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Api.Tests.Endpoints;

public sealed class WarmUpEndpointsTests
{
  private static readonly KataGoResponse _response = new("1", null, null, null, null);

  [Fact]
  public async Task WarmUp_Succeeds_ReturnsOk()
  {
    using var factory = Factory();
    using var client = factory.CreateClient();

    var response = await client.PostAsync("/warmup", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task WarmUp_ProcessExited_ReturnsInternalServerError()
  {
    using var factory = Factory(warmUpException: new InvalidOperationException("The process has exited."));
    using var client = factory.CreateClient();

    var response = await client.PostAsync("/warmup", null);

    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
  }

  [Fact]
  public async Task WarmUp_CalledOnDisposedInstance_ReturnsInternalServerError()
  {
    using var factory = Factory(warmUpException: new ObjectDisposedException(nameof(FakeKataGoClient)));
    using var client = factory.CreateClient();

    var response = await client.PostAsync("/warmup", null);

    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
  }

  // Creates a new WebApplicationFactory<Program> that overrides DI registrations with a FakeKataGoClient.
  // The FakeKataGoClient's WarmUpAsync throws warmUpException if provided, otherwise completes normally.
  private static WebApplicationFactory<Program> Factory(Exception? warmUpException = null) =>
    new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IKataGoClient>();
        services.AddSingleton<IKataGoClient>(new FakeKataGoClient(_response, warmUpException: warmUpException));
      }));
}
