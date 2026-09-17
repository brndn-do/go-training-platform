using System.Net;
using System.Net.Http.Json;
using GoTrainingPlatform.Api.Contracts;
using GoTrainingPlatform.Domain.Enums;
using Microsoft.Net.Http.Headers;

namespace GoTrainingPlatform.Api.Tests;

[Collection("PostgresApi")]
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public sealed class CorsTests(PostgresApiFixture fixture)
{
  private const string DisallowedOrigin = "https://evil.example.com";

  [Theory]
  [InlineData(PostgresApiFixture.AllowedOrigin)]
  [InlineData(PostgresApiFixture.SecondAllowedOrigin)]
  public async Task Preflight_AllowedOrigin_ReturnsAllowHeaders(string origin)
  {
    var request = PreflightRequest("/api/auth/logout", origin, HttpMethod.Post);
    var response = await SendAsync(request);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal(origin, response.Headers.GetValues(HeaderNames.AccessControlAllowOrigin).Single());
    Assert.Equal("true", response.Headers.GetValues(HeaderNames.AccessControlAllowCredentials).Single());
    Assert.Contains(HttpMethod.Post.Method, response.Headers.GetValues(HeaderNames.AccessControlAllowMethods));
  }

  [Fact]
  public async Task Preflight_DisallowedOrigin_OmitsAllowHeaders()
  {
    var request = PreflightRequest("/api/auth/logout", DisallowedOrigin, HttpMethod.Post);
    var response = await SendAsync(request);
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
    Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowCredentials));
    Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowMethods));
  }

  [Theory]
  [InlineData(PostgresApiFixture.AllowedOrigin)]
  [InlineData(PostgresApiFixture.SecondAllowedOrigin)]
  public async Task Request_AllowedOrigin_ReturnsAllowHeaders(string origin)
  {
    var request = CrossOriginRequest(HttpMethod.Post, "/api/auth/logout", origin);
    var response = await SendAsync(request);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal(origin, response.Headers.GetValues(HeaderNames.AccessControlAllowOrigin).Single());
    Assert.Equal("true", response.Headers.GetValues(HeaderNames.AccessControlAllowCredentials).Single());
  }

  [Fact]
  public async Task Request_DisallowedOrigin_OmitsAllowHeaders()
  {
    var request = CrossOriginRequest(HttpMethod.Post, "/api/auth/logout", DisallowedOrigin);
    var response = await SendAsync(request);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
    Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowCredentials));
  }

  // Starting a game reaches the fixture's unreachable engine, so GameExceptionHandler answers.
  [Theory]
  [InlineData(PostgresApiFixture.AllowedOrigin)]
  [InlineData(PostgresApiFixture.SecondAllowedOrigin)]
  public async Task Request_AllowedOriginFailingInExceptionHandler_KeepsAllowHeaders(string origin)
  {
    var client = await fixture.CreateSignedInClientAsync();
    var request = CrossOriginRequest(HttpMethod.Post, "/api/games", origin);
    request.Content = JsonContent.Create(new StartGameRequest
    {
      PlayerColor = Color.Black,
      BoardSize = 9,
      BotStrength = BotStrength.Kyu20,
    });

    var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    Assert.Equal(origin, response.Headers.GetValues(HeaderNames.AccessControlAllowOrigin).Single());
    Assert.Equal("true", response.Headers.GetValues(HeaderNames.AccessControlAllowCredentials).Single());
  }

  private static HttpRequestMessage CrossOriginRequest(HttpMethod method, string path, string origin)
  {
    var request = new HttpRequestMessage(method, path);
    request.Headers.Add(HeaderNames.Origin, origin);
    return request;
  }

  private static HttpRequestMessage PreflightRequest(string path, string origin, HttpMethod requestedMethod)
  {
    var request = CrossOriginRequest(HttpMethod.Options, path, origin);
    request.Headers.Add(HeaderNames.AccessControlRequestMethod, requestedMethod.Method);
    return request;
  }

  private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request) =>
    fixture.CreateClient().SendAsync(request);
}
