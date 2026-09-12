using Microsoft.AspNetCore.Identity;

namespace GoTrainingPlatform.Infrastructure;

/// <summary>
/// Represents an application user, extending ASP.NET Core
/// Identity's user model with a <see cref="Guid"/> primary key.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;