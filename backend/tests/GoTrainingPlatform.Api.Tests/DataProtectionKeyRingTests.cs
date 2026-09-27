using System.Net;
using System.Net.Http.Json;
using GoTrainingPlatform.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;

namespace GoTrainingPlatform.Api.Tests;

[Collection("PostgresApi")]
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public sealed class DataProtectionKeyRingTests(PostgresApiFixture fixture) : IDisposable
{
  private const string SessionCookieName = ".AspNetCore.Identity.Application";

  private const string RegisterRoute = "/api/auth/register";
  private const string LoginRoute = "/api/auth/login";
  private const string MeRoute = "/api/auth/me";

  private const string Password = "correct horse battery staple";

  private readonly List<string> _keyRingPaths = [];

  [Fact]
  public async Task SessionCookie_OnReplacementHostSharingKeyRing_StaysValid()
  {
    string keyRingPath = NewKeyRingPath();

    await using var issuingHost = fixture.CreateSeparateHost(keyRingPath);
    string email = await RegisterAsync(issuingHost);
    string cookie = await SignInAndReadCookieAsync(issuingHost, email);

    await using var replacementHost = fixture.CreateSeparateHost(keyRingPath);
    var me = await GetMeAsync(replacementHost, cookie);

    Assert.NotNull(me.User);
    Assert.Equal(email, me.User.Email);
  }

  [Fact]
  public async Task SessionCookie_OnHostWithDifferentKeyRing_IsRejected()
  {
    await using var issuingHost = fixture.CreateSeparateHost(NewKeyRingPath());
    string email = await RegisterAsync(issuingHost);
    string cookie = await SignInAndReadCookieAsync(issuingHost, email);

    await using var otherHost = fixture.CreateSeparateHost(NewKeyRingPath());
    var me = await GetMeAsync(otherHost, cookie);

    Assert.Null(me.User);
  }

  [Fact]
  public async Task SessionCookie_OnHostWithDifferentApplicationName_IsRejected()
  {
    string keyRingPath = NewKeyRingPath();

    await using var issuingHost = fixture.CreateSeparateHost(keyRingPath, applicationName: "IssuingHost");
    string email = await RegisterAsync(issuingHost);
    string cookie = await SignInAndReadCookieAsync(issuingHost, email);

    await using var otherHost = fixture.CreateSeparateHost(keyRingPath, applicationName: "OtherHost");
    var me = await GetMeAsync(otherHost, cookie);

    Assert.Null(me.User);
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    foreach (string path in _keyRingPaths)
    {
      Directory.Delete(path, recursive: true);
    }
  }

  // Cookieless, so the cookie under test is the one the test sets by hand rather than
  // whatever a container happened to keep. HTTPS because the host redirects http away.
  private static HttpClient CreateClient(WebApplicationFactory<Program> host) =>
    host.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost"),
      HandleCookies = false,
    });

  private static async Task<string> RegisterAsync(WebApplicationFactory<Program> host)
  {
    string email = $"{Guid.NewGuid():N}@example.com";

    var response = await CreateClient(host)
      .PostAsJsonAsync(RegisterRoute, new RegisterRequest { Email = email, Password = Password });

    if (response.StatusCode != HttpStatusCode.NoContent)
    {
      throw new InvalidOperationException($"Test setup failed: registering {email} returned {response.StatusCode}.");
    }

    return email;
  }

  private static async Task<string> SignInAndReadCookieAsync(WebApplicationFactory<Program> host, string email)
  {
    var response = await CreateClient(host)
      .PostAsJsonAsync(LoginRoute, new LoginRequest { Email = email, Password = Password });

    if (response.StatusCode != HttpStatusCode.NoContent)
    {
      throw new InvalidOperationException($"Test setup failed: logging in {email} returned {response.StatusCode}.");
    }

    var cookie = Assert.Single(
      response.Headers.GetValues("Set-Cookie").Select(header => SetCookieHeaderValue.Parse(header)),
      candidate => candidate.Name == SessionCookieName);

    return $"{cookie.Name}={cookie.Value}";
  }

  private static async Task<MeResponse> GetMeAsync(WebApplicationFactory<Program> host, string cookie)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, MeRoute);
    request.Headers.Add("Cookie", cookie);

    var response = await CreateClient(host).SendAsync(request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    return (await response.Content.ReadFromJsonAsync<MeResponse>())!;
  }

  private string NewKeyRingPath()
  {
    string path = Path.Combine(Path.GetTempPath(), $"keyring-{Guid.NewGuid():N}");

    Directory.CreateDirectory(path);
    _keyRingPaths.Add(path);

    return path;
  }
}
