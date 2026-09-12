using Microsoft.AspNetCore.Authentication;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// Options for <see cref="TestAuthHandler"/>.
/// </summary>
public sealed class TestAuthOptions : AuthenticationSchemeOptions
{
  /// <summary>
  /// Gets or sets the id every request is signed in as.
  /// </summary>
  public Guid UserId { get; set; }
}
