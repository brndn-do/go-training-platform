using System.Security.Claims;
using GoTrainingPlatform.Application;

namespace GoTrainingPlatform.Api;

/// <summary>
/// Supplies the id of the user signed in on the current request.
/// </summary>
/// <param name="httpContextAccessor">Provides the current request.</param>
public sealed class HttpContextCurrentPlayer(IHttpContextAccessor httpContextAccessor) : ICurrentPlayer
{
  /// <inheritdoc/>
  /// <exception cref="InvalidOperationException">
  /// There is no current request, no user is signed in on it, or the user's id claim is not a
  /// valid <see cref="Guid"/>.
  /// </exception>
  public Guid Id
  {
    get
    {
      var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No signed-in user on the current request.");

      if (!Guid.TryParse(claim.Value, out Guid id))
      {
        throw new InvalidOperationException("The user id claim is not a valid Guid.");
      }

      return id;
    }
  }
}
