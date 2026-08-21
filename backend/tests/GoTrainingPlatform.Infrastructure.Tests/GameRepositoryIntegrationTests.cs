using GoTrainingPlatform.Domain;
using GoTrainingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GoTrainingPlatform.Infrastructure.Tests;

[Collection("Postgres")]
[Trait("Category", "Integration")]
public sealed class GameRepositoryIntegrationTests(PostgresFixture postgresFixture)
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

  [Fact]
  public async Task GetByIdAsync_AnotherGamesUndoFreedRowSpace_ReturnsMovesInMoveNumberOrder()
  {
    const int movesPerGame = 300;
    const int extraMoves = 100;

    Guid gameA = Guid.NewGuid();
    Guid gameB = Guid.NewGuid();

    foreach (Guid id in new[] { gameA, gameB })
    {
      Game game = new(id, Guid.NewGuid(), Color.Black, 19, null);
      game.BuildPosition();
      await Repo().AddAsync(game);
    }

    // Setup goes through raw SQL because EF sorts its own inserts by key, and because a real
    // run reaches this state over many undos and an autovacuum. The statements are the ones
    // SaveAsync emits: removed moves are deleted, added moves are inserted, never updated.

    // Two games playing at once, so their rows interleave on the same pages.
    await Exec(
      "INSERT INTO moves (game_id, move_number, coordinates_x, coordinates_y) "
      + "SELECT CASE WHEN i % 2 = 0 THEN {0} ELSE {1} END, i / 2, 1, 1 "
      + $"FROM generate_series(0, {(movesPerGame * 2) - 1}) AS i",
      gameA,
      gameB);

    // Game A undoes back to move 250, freeing space on pages game B also sits on.
    await Exec("DELETE FROM moves WHERE game_id = {0} AND move_number < 250", gameA);
    await Exec("VACUUM moves");

    // Game B plays on. Its new rows land in the space game A gave up, physically ahead of
    // game B's own earlier moves.
    await Exec(
      "INSERT INTO moves (game_id, move_number, coordinates_x, coordinates_y) "
      + "SELECT {0}, i, 3, 3 "
      + $"FROM generate_series({movesPerGame}, {movesPerGame + extraMoves - 1}) AS i",
      gameB);

    var result = await Repo().GetByIdAsync(gameB);

    Assert.NotNull(result);
    Assert.Equal(
      Enumerable.Range(0, movesPerGame + extraMoves),
      result.Moves.Select(move => move.MoveNumber));
  }

  // Runs setup SQL on its own context, outside any transaction (VACUUM needs that).
  private async Task Exec(string sql, params object[] parameters)
  {
    await using var context = postgresFixture.CreateContext();
    await context.Database.ExecuteSqlRawAsync(sql, parameters);
  }

  // Returns a repository with a fresh context.
  private GameRepository Repo() => new(postgresFixture.CreateContext());
}