namespace GoTrainingPlatform.Application.Orchestration;

/// <summary>
/// Thrown when a bot response is invalid.
/// </summary>
public sealed class InvalidBotResponseException()
  : Exception("The bot's response was invalid.");