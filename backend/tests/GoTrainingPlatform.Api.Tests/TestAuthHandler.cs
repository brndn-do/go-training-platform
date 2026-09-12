using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// Signs every request in as <see cref="TestAuthOptions.UserId"/>, carried in the
/// <see cref="ClaimTypes.NameIdentifier"/> claim.
/// </summary>
public sealed class TestAuthHandler(
  IOptionsMonitor<TestAuthOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder)
  : AuthenticationHandler<TestAuthOptions>(options, logger, encoder)
{
  /// <summary>
  /// The name this scheme is registered under.
  /// </summary>
  public const string SchemeName = "Test";

  /// <inheritdoc/>
  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    Claim[] claims = [new(ClaimTypes.NameIdentifier, Options.UserId.ToString())];
    ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));

    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
  }
}
