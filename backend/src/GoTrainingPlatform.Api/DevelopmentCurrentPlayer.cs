using GoTrainingPlatform.Application;
using Microsoft.Extensions.Options;

namespace GoTrainingPlatform.Api;

/// <summary>
/// Supplies a single configured player id for every request. A stand-in for real
/// authentication: it must only ever be registered in the Development environment.
/// </summary>
public sealed class DevelopmentCurrentPlayer(IOptions<CurrentPlayerOptions> options) : ICurrentPlayer
{
  /// <inheritdoc/>
  public Guid Id { get; } = options.Value.Id;
}
