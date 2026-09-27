namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// A clock that only moves when told to, so a test can assert the exact time a repository
/// stamped. Whole seconds survive a round trip through Postgres, whose timestamps stop at
/// microseconds.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
  /// <summary>
  /// Gets or sets the time the clock reads.
  /// </summary>
  public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

  /// <inheritdoc/>
  public override DateTimeOffset GetUtcNow() => UtcNow;
}
