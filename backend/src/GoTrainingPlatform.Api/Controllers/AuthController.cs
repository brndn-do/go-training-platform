using System.Security.Claims;
using GoTrainingPlatform.Api.Contracts;
using GoTrainingPlatform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.Controllers;

/// <summary>
/// The authentication endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : ControllerBase
{
  /// <summary>
  /// Registers a new account. Does not sign the new user in.
  /// </summary>
  /// <param name="request">The email and password to register with.</param>
  /// <returns>No content once the account exists.</returns>
  [AllowAnonymous]
  [HttpPost("register")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> Register(RegisterRequest request)
  {
    ApplicationUser user = new() { UserName = request.Email, Email = request.Email };

    var result = await userManager.CreateAsync(user, request.Password!);

    if (result.Succeeded)
    {
      return NoContent();
    }

    // Identity reports a duplicate email or a rejected password as a result
    foreach (var error in result.Errors)
    {
      ModelState.AddModelError(error.Code, error.Description);
    }

    return ValidationProblem(ModelState);
  }

  /// <summary>
  /// Signs in to an existing account, issuing the session cookie.
  /// </summary>
  /// <param name="request">The email and password to sign in with.</param>
  /// <returns>No content once the session cookie is issued.</returns>
  [AllowAnonymous]
  [HttpPost("login")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> Login(LoginRequest request)
  {
    var result = await signInManager.PasswordSignInAsync(
      request.Email!,
      request.Password!,
      request.RememberMe,
      lockoutOnFailure: true);

    if (result.Succeeded)
    {
      return NoContent();
    }

    // Every failure answers the same way. Distinguishing a locked-out or unknown account
    // from a wrong password would say whether that account exists.
    return Unauthorized();
  }

  /// <summary>
  /// Signs out, clearing the session cookie. Succeeds whether or not a session existed.
  /// </summary>
  /// <returns>No content.</returns>
  [AllowAnonymous]
  [HttpPost("logout")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  public async Task<IActionResult> Logout()
  {
    await signInManager.SignOutAsync();

    return NoContent();
  }

  /// <summary>
  /// Reports who, if anyone, is signed in on the current request.
  /// </summary>
  /// <returns>A <see cref="MeResponse"/> whose <c>User</c> is <c>null</c> when no one is signed in.</returns>
  [AllowAnonymous]
  [HttpGet("me")]
  [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
  [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
  public IActionResult Me()
  {
    bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;

    if (!isAuthenticated)
    {
      return Ok(new MeResponse(null));
    }

    // Authenticated but unreadable
    if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id))
    {
      throw new InvalidOperationException("The user id claim is not a valid Guid.");
    }

    var email = User.FindFirstValue(ClaimTypes.Email)
      ?? throw new InvalidOperationException("The signed-in user has no email claim.");

    return Ok(new MeResponse(new UserResponse(id, email)));
  }
}