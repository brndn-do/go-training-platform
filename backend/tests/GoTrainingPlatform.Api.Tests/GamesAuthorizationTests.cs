using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.Tests;

[Collection("PostgresApi")]
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public sealed class GamesAuthorizationTests(PostgresApiFixture fixture)
{
  // Against the real host, so the 401 comes from the session cookie's own scheme. A test
  // authentication scheme answers 401 by itself, however the cookie is configured.
  [Theory]
  [InlineData("POST", "/api/games")]
  [InlineData("GET", "/api/games/{0}")]
  [InlineData("POST", "/api/games/{0}/resume")]
  [InlineData("POST", "/api/games/{0}/moves")]
  [InlineData("POST", "/api/games/{0}/pass")]
  [InlineData("POST", "/api/games/{0}/undo")]
  [InlineData("POST", "/api/games/{0}/resign")]
  public async Task GameRoute_NotSignedIn_ReturnsUnauthorizedProblem(string method, string route)
  {
    using HttpRequestMessage request = new(new HttpMethod(method), string.Format(route, Guid.NewGuid()));

    var response = await fixture.CreateClient().SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

    Assert.Equal((int)HttpStatusCode.Unauthorized, problem!.Status);
  }

  [Fact]
  public async Task GameRoute_SignedIn_ReturnsNotFoundRatherThanUnauthorized()
  {
    // Without this, the test above would also pass if the session cookie never worked at all.
    var client = await fixture.CreateSignedInClientAsync();

    var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }
}
