using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace GoTrainingPlatform.Api.Tests;

public sealed class HttpContextCurrentPlayerTests
{
  [Fact]
  public void Id_SignedInUser_ReturnsIdFromClaim()
  {
    Guid userId = Guid.NewGuid();
    var currentPlayer = CurrentPlayerFor(SignedIn(new Claim(ClaimTypes.NameIdentifier, userId.ToString())));

    Assert.Equal(userId, currentPlayer.Id);
  }

  [Fact]
  public void Id_NoSignedInUser_Throws()
  {
    // A request with nobody signed in carries an empty, unauthenticated principal.
    var currentPlayer = CurrentPlayerFor(new DefaultHttpContext());

    Assert.Throws<InvalidOperationException>(() => currentPlayer.Id);
  }

  [Fact]
  public void Id_NoHttpContext_Throws()
  {
    // Set explicitly: the accessor stores its value in a static AsyncLocal, so it is not
    // guaranteed to start out empty.
    var currentPlayer = new HttpContextCurrentPlayer(new HttpContextAccessor { HttpContext = null });

    Assert.Throws<InvalidOperationException>(() => currentPlayer.Id);
  }

  [Fact]
  public void Id_ClaimIsNotAGuid_Throws()
  {
    var currentPlayer = CurrentPlayerFor(SignedIn(new Claim(ClaimTypes.NameIdentifier, "not-a-guid")));

    Assert.Throws<InvalidOperationException>(() => currentPlayer.Id);
  }

  // Authenticated under Identity's application scheme, as the cookie handler leaves it.
  private static DefaultHttpContext SignedIn(params Claim[] claims) =>
    new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)) };

  private static HttpContextCurrentPlayer CurrentPlayerFor(HttpContext context) =>
    new(new HttpContextAccessor { HttpContext = context });
}
