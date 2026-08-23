namespace GoTrainingPlatform.Application.Tests;

public sealed class FakeCurrentPlayer(Guid id) : ICurrentPlayer
{
  public Guid Id { get; } = id;
}