namespace GoTrainingPlatform.Application;

/// <summary>
/// Thrown when a bot response is invalid.
/// </summary>
public class InvalidBotResponseException()
  : Exception("The bot's response was invalid.");