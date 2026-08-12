namespace Engine.Api.Analysis;

/// <summary>
/// Thrown when a bot response is invalid.
/// </summary>
public sealed class InvalidKataGoResponseException(string message)
  : Exception(message);
