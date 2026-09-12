using System.Net;
using System.Net.Http.Json;
using GoTrainingPlatform.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace GoTrainingPlatform.Api.Tests;

[Collection("PostgresApi")]
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public sealed class AuthEndpointsTests(PostgresApiFixture fixture)
{
  // The name ASP.NET Core gives Identity's application cookie by default.
  private const string SessionCookieName = ".AspNetCore.Identity.Application";

  [Fact]
  public async Task Register_NewAccount_PersistsUser()
  {
    string email = NewEmail();

    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "correct horse battery staple" });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    await using var context = fixture.CreateContext();

    Assert.True(await context.Users.AnyAsync(user => user.Email == email));
  }

  [Fact]
  public async Task Register_DuplicateEmail_ReturnsBadRequest()
  {
    string email = NewEmail();

    var response1 = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "correct horse battery staple" });

    Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

    var response2 = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "correct horse battery staple" });

    Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

    var problem = await response2.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Contains("DuplicateEmail", problem!.Errors.Keys);

    await using var context = fixture.CreateContext();

    Assert.Equal(1, await context.Users.CountAsync(user => user.Email == email));
  }

  [Fact]
  public async Task Register_ShortPassword_ReturnsBadRequestAndDoesNotPersist()
  {
    string email = NewEmail();

    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = "short" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.Contains("PasswordTooShort", problem!.Errors.Keys);

    await using var context = fixture.CreateContext();

    Assert.False(await context.Users.AnyAsync(user => user.Email == email));
  }

  [Fact]
  public async Task Login_CorrectCredentials_ReturnsNoContent()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task Login_WrongPassword_ReturnsUnauthorized()
  {
    string email = NewEmail();
    await RegisterAsync(email, "correct horse battery staple");

    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "wrong password entirely" });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Login_UnknownEmail_ReturnsUnauthorized()
  {
    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = NewEmail(), Password = "correct horse battery staple" });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Login_WrongPasswordVsUnknownEmail_ReturnsIdenticalResponse()
  {
    string registeredEmail = NewEmail();
    await RegisterAsync(registeredEmail, "correct horse battery staple");

    var wrongPasswordResponse = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = registeredEmail, Password = "wrong password entirely" });

    var unknownEmailResponse = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = NewEmail(), Password = "correct horse battery staple" });

    Assert.Equal(wrongPasswordResponse.StatusCode, unknownEmailResponse.StatusCode);

    var wrongPassword = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemDetails>();
    var unknownEmail = await unknownEmailResponse.Content.ReadFromJsonAsync<ProblemDetails>();

    // Compared field by field rather than as whole bodies: traceId is per-request, so it
    // differs between any two responses and says nothing about which account exists.
    // Comparing the extension keys still catches a future extension that would.
    Assert.Equal(wrongPassword!.Status, unknownEmail!.Status);
    Assert.Equal(wrongPassword.Title, unknownEmail.Title);
    Assert.Equal(wrongPassword.Type, unknownEmail.Type);
    Assert.Equal(wrongPassword.Detail, unknownEmail.Detail);
    Assert.Equal(wrongPassword.Extensions.Keys, unknownEmail.Extensions.Keys);
  }

  [Fact]
  public async Task Login_CorrectCredentials_SetsCookieWithSecureAttributes()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var response = await fixture.CreateCookielessClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
    var cookie = SetCookieHeaderValue.Parse(cookies!.Single());

    Assert.True(cookie.HttpOnly);
    Assert.True(cookie.Secure);
    Assert.Equal(SameSiteMode.Lax, cookie.SameSite);

    // No Domain attribute is what makes the cookie host-only, per ADR 29.
    Assert.False(cookie.Domain.HasValue);
  }

  [Fact]
  public async Task Login_RememberMeTrue_CookieIsPersistent()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var response = await fixture.CreateCookielessClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password, RememberMe = true });

    var cookie = SetCookieHeaderValue.Parse(response.Headers.GetValues("Set-Cookie").Single());

    Assert.True(cookie.Expires.HasValue || cookie.MaxAge.HasValue);
  }

  [Fact]
  public async Task Login_RememberMeFalse_CookieIsSession()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var response = await fixture.CreateCookielessClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password, RememberMe = false });

    var cookie = SetCookieHeaderValue.Parse(response.Headers.GetValues("Set-Cookie").Single());

    Assert.Null(cookie.Expires);
    Assert.Null(cookie.MaxAge);
  }

  [Fact(Skip = "Needs an [Authorize]-protected route to probe, which lands with piece 5.")]
  public async Task Login_WrongPassword_DoesNotAuthenticateSubsequentRequest()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var client = fixture.CreateClient();

    await client.PostAsJsonAsync(
      "/api/auth/login",
      new LoginRequest { Email = email, Password = "wrong password entirely" });

    // TODO: this needs an authenticated endpoint to probe with, which does not exist until
    // [Authorize] lands. Revisit once piece 5 adds a protected route to check against.
    Assert.Fail("TODO: deferred until an authenticated endpoint exists.");
  }

  [Fact]
  public async Task Login_WrongPassword_IncrementsFailedAccessCount()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "wrong password entirely" });

    Assert.Equal(1, await ReadAccessFailedCountAsync(email));
  }

  [Fact]
  public async Task Login_SucceedsAfterFailure_ResetsFailedAccessCount()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var client = fixture.CreateClient();

    await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "wrong password entirely" });

    // Checked before signing in, so a reset is what this observes rather than a count that
    // was never incremented in the first place.
    Assert.Equal(1, await ReadAccessFailedCountAsync(email));

    var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal(0, await ReadAccessFailedCountAsync(email));
  }

  [Fact]
  public async Task Logout_AfterLogin_ExpiresSessionCookie()
  {
    string email = NewEmail();
    const string password = "correct horse battery staple";
    await RegisterAsync(email, password);

    var client = fixture.CreateClient();

    var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);

    var response = await client.PostAsync("/api/auth/logout", content: null);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var cookie = ReadSessionCookie(response);

    Assert.Equal(string.Empty, cookie.Value.ToString());
    Assert.NotNull(cookie.Expires);
    Assert.True(cookie.Expires < DateTimeOffset.UtcNow, "The session cookie should expire in the past.");
  }

  [Fact]
  public async Task Logout_WithoutSession_ReturnsNoContent()
  {
    var response = await fixture.CreateClient().PostAsync("/api/auth/logout", content: null);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  // Each test needs an address no other test has used, since the fixture's database is
  // shared across the collection and emails are unique.
  private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

  // Signing out clears every Identity scheme, so the response carries a Set-Cookie for each
  // of them. Only the application scheme's cookie carries the session.
  private static SetCookieHeaderValue ReadSessionCookie(HttpResponseMessage response) =>
    Assert.Single(
      response.Headers.GetValues("Set-Cookie").Select(header => SetCookieHeaderValue.Parse(header)),
      cookie => cookie.Name == SessionCookieName);

  // Reads through a fresh context every time: a reused one would answer from its identity
  // map and hide the write the endpoint actually made.
  private async Task<int> ReadAccessFailedCountAsync(string email)
  {
    await using var context = fixture.CreateContext();

    return await context.Users
      .Where(candidate => candidate.Email == email)
      .Select(candidate => candidate.AccessFailedCount)
      .FirstAsync();
  }

  // Registers through the real endpoint rather than inserting a row directly, so the
  // password hash a login test verifies against is the one Register actually produces.
  private async Task RegisterAsync(string email, string password)
  {
    var response = await fixture.CreateClient()
      .PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });

    if (response.StatusCode != HttpStatusCode.NoContent)
    {
      throw new InvalidOperationException($"Test setup failed: registering {email} returned {response.StatusCode}.");
    }
  }
}
