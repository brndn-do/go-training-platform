using System.ComponentModel.DataAnnotations;

namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// A request to register a user account.
/// </summary>
public sealed record RegisterRequest
{
  /// <summary>
  /// Gets the email, which is also the username. Required.
  /// </summary>
  [Required]
  [EmailAddress]
  [StringLength(256)]
  public string? Email { get; init; }

  /// <summary>
  /// Gets the password. Required. The minimum length is enforced by Identity, not here.
  /// </summary>
  [Required]
  [StringLength(256)]
  public string? Password { get; init; }
}
