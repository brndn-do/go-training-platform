using System.Net;
using System.Net.Http.Json;
using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Api.Tests.Endpoints;

public sealed class SuggestionEndpointsTests
{
  private const int BoardSize = 9;

  [Fact]
  public async Task GetSuggestion_ValidSuperhumanRequest_ReturnsOk()
  {
    // Superhuman picks via argmax (deterministic), unlike Kyu/Dan which samples via Random —
    // policy[0] is the only nonzero entry, so index 0 always wins.
    double[] policy = new double[(BoardSize * BoardSize) + 1];
    policy[0] = 1.0;

    KataGoResponse fakeResponse = new("test", null, policy, null, new KataGoRootInfo(0.55, null));

    using var factory = Factory(fakeResponse);
    using var client = factory.CreateClient();

    SuggestionRequest request = new([], BoardSize, 7.5, "Superhuman");

    var httpResponse = await client.PostAsJsonAsync("/suggestion", request);

    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

    SuggestionResponse? suggestion = await httpResponse.Content.ReadFromJsonAsync<SuggestionResponse>();
    Assert.NotNull(suggestion);
    Assert.NotNull(suggestion.Move);
    Assert.Equal(0, suggestion.Move!.X);
    Assert.Equal(BoardSize - 1, suggestion.Move!.Y);
    Assert.Equal(0.55, suggestion.BlackWinRate);
  }

  [Fact]
  public async Task GetSuggestion_KataGoReturnsError_ReturnsInternalServerError()
  {
    KataGoResponse fakeResponse = new("test", "KataGo rejected the query.", null, null, null);

    using var factory = Factory(fakeResponse);
    using var client = factory.CreateClient();

    SuggestionRequest request = new([], BoardSize, 7.5, "Superhuman");

    var httpResponse = await client.PostAsJsonAsync("/suggestion", request);

    Assert.Equal(HttpStatusCode.InternalServerError, httpResponse.StatusCode);
  }

  [Fact]
  public async Task GetSuggestion_InvalidBotStrength_ReturnsBadRequestWithDetail()
  {
    KataGoResponse fakeResponse = new("test", null, null, null, null);

    using var factory = Factory(fakeResponse);
    using var client = factory.CreateClient();

    SuggestionRequest request = new([], BoardSize, 7.5, "NotAValidStrength");

    var httpResponse = await client.PostAsJsonAsync("/suggestion", request);

    Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

    string body = await httpResponse.Content.ReadAsStringAsync();
    Assert.Contains("Invalid bot strength", body);
  }

  private static WebApplicationFactory<Program> Factory(KataGoResponse response) =>
    new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IKataGoClient>();
        services.AddSingleton<IKataGoClient>(new FakeKataGoClient(response));
      }));
}
