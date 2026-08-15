namespace Engine.Api.Analysis;

/// <summary>
/// Thrown when KataGo's response is invalid.
/// </summary>
public sealed class InvalidKataGoResponseException(string message)
  : Exception(message);
