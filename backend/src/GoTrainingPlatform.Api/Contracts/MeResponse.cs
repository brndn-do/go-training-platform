namespace GoTrainingPlatform.Api.Contracts;

/// <summary>
/// Answers who, if anyone, is signed in on the current request.
/// </summary>
/// <param name="User"><c>null</c> when no one is signed in.</param>
public sealed record MeResponse(UserResponse? User);
