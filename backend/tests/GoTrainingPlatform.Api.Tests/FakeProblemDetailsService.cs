using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// Records the problem details it is asked to write instead of writing them, so a test can
/// inspect the body the handler produced without going through the HTTP pipeline.
/// </summary>
public sealed class FakeProblemDetailsService : IProblemDetailsService
{
  /// <summary>Gets the details of the last write, or null if there has been none.</summary>
  public ProblemDetails? Written { get; private set; }

  /// <summary>Gets the number of times a write was attempted.</summary>
  public int WriteCount { get; private set; }

  public ValueTask WriteAsync(ProblemDetailsContext context)
  {
    Written = context.ProblemDetails;
    WriteCount++;
    return ValueTask.CompletedTask;
  }

  public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
  {
    Written = context.ProblemDetails;
    WriteCount++;
    return ValueTask.FromResult(true);
  }
}
