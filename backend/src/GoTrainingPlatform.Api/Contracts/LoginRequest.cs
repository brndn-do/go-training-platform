using System.ComponentModel.DataAnnotations;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A request to sign in to a user account.
/// </summary>
public sealed record LoginRequest
{
  /// <summary>
  /// Gets the email, which is also the username. Required.
  /// </summary>
  [Required]
  [EmailAddress]
  [StringLength(256)]
  public string? Email { get; init; }

  /// <summary>
  /// Gets the password. Required.
  /// </summary>
  [Required]
  [StringLength(256)]
  public string? Password { get; init; }

  /// <summary>
  /// Gets a value indicating whether the session outlives the browser session.
  /// Defaults to false when omitted.
  /// </summary>
  public bool RememberMe { get; init; }
}
