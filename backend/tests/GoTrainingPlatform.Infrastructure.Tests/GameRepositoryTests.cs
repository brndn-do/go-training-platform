using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure.Tests;

[Collection("Postgres")]
public sealed class GameRepositoryTests(PostgresFixture postgresFixture)
{
  [Fact]
  public async Task GetByIdAsync_NonExistentId_ReturnsNull()
  {
    var result = await Repo().GetByIdAsync(Guid.NewGuid());
    Assert.Null(result);
  }

  [Fact]
  public async Task AddAsync_NewGame_PersistsGame()
  {
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    await Repo().AddAsync(game);
    var result = await Repo().GetByIdAsync(gameId);

    Assert.NotNull(result);
    result.BuildPosition();
    Assert.Equivalent(game, result);
  }

  [Fact]
  public async Task AddAsync_InProgressGame_PersistsGame()
  {
    Guid gameId = Guid.NewGuid();
    Game game = new([new Move(new Coordinates(0, 0), 1)], gameId, Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    await Repo().AddAsync(game);
    var result = await Repo().GetByIdAsync(gameId);

    Assert.NotNull(result);
    result.BuildPosition();
    Assert.Equivalent(game, result);
  }

  [Fact]
  public async Task SaveAsync_AfterMove_PersistsGame()
  {
    Guid gameId = Guid.NewGuid();
    Game newGame = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    newGame.BuildPosition();

    await Repo().AddAsync(newGame);

    var existingGame = await Repo().GetByIdAsync(gameId);
    Assert.NotNull(existingGame);
    existingGame.BuildPosition();

    Assert.True(existingGame.TryRecordMove(Color.Black, 0, 0));

    await Repo().SaveAsync(existingGame);

    var result = await Repo().GetByIdAsync(gameId);

    Assert.NotNull(result);
    result.BuildPosition();
    Assert.Equivalent(existingGame, result);
  }

  [Fact]
  public async Task SaveAsync_AfterUndo_PersistsGame()
  {
    Guid gameId = Guid.NewGuid();

    // Game with a move to undo
    Game newGame = new([new Move(new Coordinates(0, 0), 1)], gameId, Guid.NewGuid(), Color.Black, 9, null);
    newGame.BuildPosition();

    await Repo().AddAsync(newGame);

    var existingGame = await Repo().GetByIdAsync(gameId);
    Assert.NotNull(existingGame);
    existingGame.BuildPosition();

    Assert.True(existingGame.TryUndo());

    Assert.Empty(existingGame.Moves);

    await Repo().SaveAsync(existingGame);

    var result = await Repo().GetByIdAsync(gameId);

    Assert.NotNull(result);
    result.BuildPosition();
    Assert.Equivalent(existingGame, result);
  }

  [Fact]
  public async Task SaveAsync_AfterManyUndosAndMoves_PersistsGame()
  {
    Guid gameId = Guid.NewGuid();
    Game newGame = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    newGame.BuildPosition();

    for (int i = 0; i < 9; i++)
    {
      var color = i % 2 == 0 ? Color.Black : Color.White;
      Assert.True(newGame.TryRecordMove(color, 0, i));
    }

    await Repo().AddAsync(newGame);

    var existingGame = await Repo().GetByIdAsync(gameId);
    Assert.NotNull(existingGame);
    existingGame.BuildPosition();

    Assert.True(existingGame.TryUndo());
    Assert.True(existingGame.TryUndo());
    Assert.True(existingGame.TryUndo());

    for (int i = 0; i < 9; i++)
    {
      var color = i % 2 == 0 ? Color.Black : Color.White;
      Assert.True(existingGame.TryRecordMove(color, 1, i));
    }

    await Repo().SaveAsync(existingGame);

    var result = await Repo().GetByIdAsync(gameId);

    Assert.NotNull(result);
    result.BuildPosition();
    Assert.Equivalent(existingGame, result);
  }

  [Fact]
  public async Task SaveAsync_Concurrent_ThrowsDbUpdateConcurrencyException()
  {
    Guid gameId = Guid.NewGuid();
    Game game = new(gameId, Guid.NewGuid(), Color.Black, 9, null);
    game.BuildPosition();

    await Repo().AddAsync(game);

    var repo1 = Repo();
    var repo2 = Repo();

    var result1 = await repo1.GetByIdAsync(gameId);
    var result2 = await repo2.GetByIdAsync(gameId);

    Assert.NotNull(result1);
    Assert.NotNull(result2);

    result1.BuildPosition();
    result1.TryRecordResign(Color.Black);
    result2.BuildPosition();
    result2.TryRecordResign(Color.Black);

    await repo1.SaveAsync(result1);
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repo2.SaveAsync(result2));
  }

  // Returns a repository with a fresh context.
  private GameRepository Repo() => new(postgresFixture.CreateContext());
}