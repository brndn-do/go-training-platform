using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.ErrorHandling;

/// <summary>
/// Maps the failures the application layer can throw onto HTTP responses. Anything it does
/// not recognise is left for the framework to report as a generic 500.
/// </summary>
/// <param name="problemDetailsService">Writes the response body.</param>
/// <param name="logger">Records the failure, including detail kept out of the response.</param>
public sealed class GameExceptionHandler(
  IProblemDetailsService problemDetailsService,
  ILogger<GameExceptionHandler> logger) : IExceptionHandler
{
  private const string TryAgain =
    "The service is temporarily unable to handle this. Try again shortly.";

  private const string Unrecoverable =
    "Something went wrong on our end. Repeating this request will not help.";

  /// <inheritdoc/>
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
  {
    // Cancellation is not a failure
    if (exception is OperationCanceledException)
    {
      logger.LogInformation("Request was cancelled.");
      return true;
    }

    (int StatusCode, string Detail)? outcome = Map(exception);
    if (outcome is null)
    {
      return false;
    }

    // The exception's own message names operations and carries SQLSTATEs. It belongs in the
    // log, never in the body.
    logger.LogError(exception, "Request failed with {StatusCode}.", outcome.Value.StatusCode);

    httpContext.Response.StatusCode = outcome.Value.StatusCode;

    return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = exception,
      ProblemDetails = new ProblemDetails
      {
        Status = outcome.Value.StatusCode,
        Detail = outcome.Value.Detail,
      },
    });
  }

  // Returns the status and the client-facing detail for a failure this handler owns, or null
  // for one it does not. Details say what the client should do, never what went wrong
  // internally.
  private static (int StatusCode, string Detail)? Map(Exception exception) => exception switch
  {
    RepositoryException repositoryException => MapRepositoryException(repositoryException),
    EngineException engineException => MapEngineException(engineException),
    InvalidBotResponseException => (StatusCodes.Status500InternalServerError, Unrecoverable),
    _ => null,
  };

  private static (int StatusCode, string Detail) MapRepositoryException(RepositoryException exception) => exception.Kind switch
  {
    RepositoryFailureKind.Conflict => (
      StatusCodes.Status409Conflict,
      "This game changed since you loaded it. Load it again before acting on it — part of "
      + "this request may already have been saved, so sending it again is not safe."),
    RepositoryFailureKind.Unavailable => (StatusCodes.Status503ServiceUnavailable, TryAgain),
    RepositoryFailureKind.Rejected => (StatusCodes.Status500InternalServerError, Unrecoverable),
    _ => throw new ArgumentOutOfRangeException(nameof(exception), exception.Kind, "Unknown kind."),
  };

  private static (int StatusCode, string Detail) MapEngineException(EngineException exception) => exception.Kind switch
  {
    EngineFailureKind.Unavailable => (StatusCodes.Status503ServiceUnavailable, TryAgain),
    EngineFailureKind.InvalidRequest => (StatusCodes.Status500InternalServerError, Unrecoverable),
    EngineFailureKind.InvalidResponse => (StatusCodes.Status500InternalServerError, Unrecoverable),
    _ => throw new ArgumentOutOfRangeException(nameof(exception), exception.Kind, "Unknown kind."),
  };
}
