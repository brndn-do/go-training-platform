using GoTrainingPlatform.Api.ErrorHandling;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// The handler decides what every client sees when something throws, so these exercise it
/// directly rather than through an endpoint — the mapping table is cheaper to cover here,
/// and a status is easier to read than a response body.
/// </summary>
public sealed class GameExceptionHandlerTests
{
  [Theory]
  [InlineData(RepositoryFailureKind.Conflict, StatusCodes.Status409Conflict)]
  [InlineData(RepositoryFailureKind.Unavailable, StatusCodes.Status503ServiceUnavailable)]
  [InlineData(RepositoryFailureKind.Rejected, StatusCodes.Status500InternalServerError)]
  public async Task TryHandleAsync_RepositoryFailure_ReportsTheExpectedStatus(
    RepositoryFailureKind kind,
    int expectedStatus)
  {
    var (handler, context, problems) = Subject();
    RepositoryException exception = new(kind, "Saving a game failed (40001).");

    bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(expectedStatus, context.Response.StatusCode);

    // The status is set on the response and again on the body. A client reading either must
    // not be told something different from the other.
    Assert.NotNull(problems.Written);
    Assert.Equal(expectedStatus, problems.Written.Status);
  }

  [Theory]
  [InlineData(EngineFailureKind.Unavailable, StatusCodes.Status503ServiceUnavailable)]
  [InlineData(EngineFailureKind.InvalidRequest, StatusCodes.Status500InternalServerError)]
  [InlineData(EngineFailureKind.InvalidResponse, StatusCodes.Status500InternalServerError)]
  public async Task TryHandleAsync_EngineFailure_ReportsTheExpectedStatus(
    EngineFailureKind kind,
    int expectedStatus)
  {
    var (handler, context, problems) = Subject();
    EngineException exception = new(kind, "The engine is unavailable.");

    bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(expectedStatus, context.Response.StatusCode);

    Assert.NotNull(problems.Written);
    Assert.Equal(expectedStatus, problems.Written.Status);
  }

  [Fact]
  public async Task TryHandleAsync_InvalidBotResponse_ReportsInternalServerError()
  {
    // The bot returned a move the rules rejected. A defect on our side, and nothing a client
    // can act on.
    var (handler, context, problems) = Subject();
    InvalidBotResponseException exception = new();

    bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

    Assert.NotNull(problems.Written);
    Assert.Equal(StatusCodes.Status500InternalServerError, problems.Written.Status);
  }

  [Fact]
  public async Task TryHandleAsync_MappedFailure_DoesNotPutTheExceptionMessageInTheBody()
  {
    var (handler, context, problems) = Subject();
    var message = "The engine could not be reached.";
    EngineException exception = new(EngineFailureKind.Unavailable, message);

    await handler.TryHandleAsync(context, exception, CancellationToken.None);

    // Written is checked first because DoesNotContain passes against a null actual
    Assert.NotNull(problems.Written);
    Assert.DoesNotContain(message, problems.Written.Detail);
  }

  [Fact]
  public async Task TryHandleAsync_Cancellation_ClaimsItWithoutWritingAResponse()
  {
    // A client that has gone away cannot be told anything. If this regresses, every
    // abandoned request becomes a 500 in the logs and metrics — silently, since nothing
    // about the successful requests changes.
    var (handler, context, problems) = Subject();
    int statusBefore = context.Response.StatusCode;

    bool handled = await handler.TryHandleAsync(
      context, new OperationCanceledException(), CancellationToken.None);

    // Claimed, so the framework's fallback does not step in and report a server error.
    Assert.True(handled);
    Assert.Equal(0, problems.WriteCount);
    Assert.Equal(statusBefore, context.Response.StatusCode);
  }

  [Fact]
  public async Task TryHandleAsync_UnmappedFailure_DeclinesSoTheFrameworkReportsIt()
  {
    // Declining leaves the exception to UseExceptionHandler's fallback, which produces a
    // generic ProblemDetails 500 and logs the stack trace. Claiming it would mean dressing
    // an unknown failure up as a known one.
    var (handler, context, problems) = Subject();
    int statusBefore = context.Response.StatusCode;

    bool handled = await handler.TryHandleAsync(
      context,
      new InvalidOperationException("A defect, not a failure this handler owns."),
      CancellationToken.None);

    Assert.False(handled);
    Assert.Equal(0, problems.WriteCount);

    // Nothing written and nothing set, so the fallback starts from a clean response.
    Assert.Equal(statusBefore, context.Response.StatusCode);
  }

  // A handler over a fresh context, with the fake so a test can read what was written.
  private static (GameExceptionHandler Handler, DefaultHttpContext Context, FakeProblemDetailsService Problems) Subject()
  {
    FakeProblemDetailsService problems = new();
    GameExceptionHandler handler = new(problems, NullLogger<GameExceptionHandler>.Instance);
    return (handler, new DefaultHttpContext(), problems);
  }
}
