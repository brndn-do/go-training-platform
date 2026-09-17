namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// The signed-in user.
/// </summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email.</param>
public sealed record UserResponse(Guid Id, string Email);
