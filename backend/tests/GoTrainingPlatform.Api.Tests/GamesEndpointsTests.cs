using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoTrainingPlatform.Api.Contracts;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Application.Tests.Games;
using GoTrainingPlatform.Application.Tests.Orchestration;
using GoTrainingPlatform.Domain.Enums;
using GoTrainingPlatform.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoTrainingPlatform.Api.Tests;

public sealed class GamesEndpointsTests
{
  private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  [Fact]
  public async Task Start_ValidRequest_ReturnsCreatedWithGameStateAndLocation()
  {
    using var factory = Factory([Hint()]);
    using var client = factory.CreateClient();

    StartGameRequest request = new() { PlayerColor = Color.Black, BoardSize = 9, BotStrength = BotStrength.Kyu20 };
    var response = await client.PostAsJsonAsync("/api/games", request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    // Check that an enum gets serialized to a string, not a number.
    Assert.Contains(
      """
      "playerColor":"Black"
      """,
      await response.Content.ReadAsStringAsync());

    GameResponse game = await ReadGameAsync(response);
    Assert.Null(game.LastMove);
    Assert.NotNull(game.Suggestion);

    // Following Location is what proves CreatedAtAction named the route value correctly.
    var location = response.Headers.Location!.ToString();
    Assert.Contains($"/api/games/{game.Id}", location);
    Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(location)).StatusCode);
  }

  [Theory]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(14)]
  [InlineData(20)]
  public async Task Start_InvalidBoardSize_ReturnsBadRequest(int boardSize)
  {
    using var factory = Factory([Hint()]);
    using var client = factory.CreateClient();

    StartGameRequest request = new() { PlayerColor = Color.Black, BoardSize = boardSize, BotStrength = BotStrength.Kyu20 };
    var response = await client.PostAsJsonAsync("/api/games", request);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Theory]
  [InlineData(null, 9, BotStrength.Superhuman)]
  [InlineData(Color.Black, null, BotStrength.Superhuman)]
  [InlineData(Color.Black, 9, null)]
  public async Task Start_MissingRequiredValues_ReturnsBadRequest(Color? playerColor, int? boardSize, BotStrength? botStrength)
  {
    using var factory = Factory([Hint()]);
    using var client = factory.CreateClient();

    StartGameRequest request = new() { PlayerColor = playerColor, BoardSize = boardSize, BotStrength = botStrength };
    var response = await client.PostAsJsonAsync("/api/games", request);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Get_ExistingGame_ReturnsGameWithoutCallingEngineOrPersisting()
  {
    // One suggestion, which starting the game consumes. FakeEngineClient throws once it runs
    // out, so a 200 here is itself the proof that Get never reached the engine.
    FakeGameRepository repository = new();
    using var factory = Factory([Hint()], repository);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);
    int savesBefore = repository.SaveAsyncCallCount;

    var response = await client.GetAsync($"/api/games/{gameId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(savesBefore, repository.SaveAsyncCallCount);

    GameResponse game = await ReadGameAsync(response);
    Assert.Null(game.Suggestion);
  }

  [Fact]
  public async Task Get_UnknownGame_ReturnsNotFound()
  {
    using var factory = Factory([]);
    using var client = factory.CreateClient();

    var response = await client.GetAsync($"/api/games/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Resume_ExistingGame_ReturnsSuggestion()
  {
    // A null suggestion would mean Resume had been routed to GameService like Get, and had
    // stopped advancing the bot's turn.
    using var factory = Factory([Hint(), Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    var response = await client.PostAsync($"/api/games/{gameId}/resume", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    GameResponse game = await ReadGameAsync(response);
    Assert.NotNull(game.Suggestion);
  }

  [Theory]
  [InlineData(null, null)]
  [InlineData(null, 0)]
  [InlineData(0, null)]
  [InlineData(-1, 0)]
  [InlineData(0, 19)]
  public async Task Move_InvalidCoordinates_ReturnsBadRequest(int? x, int? y)
  {
    // An omitted coordinate would otherwise bind to zero and play a real move at the corner.
    using var factory = Factory([Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    var response = await client.PostAsJsonAsync(
      $"/api/games/{gameId}/moves", new MoveRequest { X = x, Y = y });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Move_ValidMove_ReturnsOkWithStoneAtTheGivenPoint()
  {
    // An asymmetric point, so swapping x and y anywhere between the request and the board
    // snapshot would fail this test. Every other board assertion is on a symmetric position.
    using var factory = Factory([Hint(), Hint(), Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    var response = await client.PostAsJsonAsync(
      $"/api/games/{gameId}/moves", new MoveRequest { X = 0, Y = 1 });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    GameResponse game = await ReadGameAsync(response);
    Assert.Equal(Content.Black, game.Board[0][1]);
    Assert.Equal(Content.Empty, game.Board[1][0]);
  }

  [Fact]
  public async Task Move_PointAlreadyPlayed_ReturnsBadRequest()
  {
    // The replay is rejected before the bot is asked to answer, so it needs no suggestion.
    using var factory = Factory([Hint(), Hint(), Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    MoveRequest move = new() { X = 0, Y = 1 };
    Assert.Equal(
      HttpStatusCode.OK,
      (await client.PostAsJsonAsync($"/api/games/{gameId}/moves", move)).StatusCode);

    var response = await client.PostAsJsonAsync($"/api/games/{gameId}/moves", move);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Pass_BotAlsoPasses_EndsGameAndReportsThePassWithoutCoordinates()
  {
    // A pass leaves nothing on the board, so null coordinates on the last move are the only
    // way a client can tell one happened. The third hint is the one the orchestrator fetches
    // even though the game is now over.
    using var factory = Factory([Hint(), PassHint(), Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    var response = await client.PostAsync($"/api/games/{gameId}/pass", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    GameResponse game = await ReadGameAsync(response);
    Assert.Equal(Outcome.TwoConsecutivePasses, game.Outcome);
    Assert.NotNull(game.LastMove);
    Assert.Null(game.LastMove.Coordinates);
  }

  [Fact]
  public async Task Resign_InProgressGame_ResignsForThePlayerAndNotTheBot()
  {
    // The client sends nothing identifying, so the only colour the API can resign for is the
    // player's. A BotResigned outcome here would mean the human had won by forfeit.
    using var factory = Factory([Hint(), Hint()]);
    using var client = factory.CreateClient();
    var gameId = await StartGameAsync(client);

    var response = await client.PostAsync($"/api/games/{gameId}/resign", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    GameResponse game = await ReadGameAsync(response);
    Assert.Equal(Outcome.PlayerResigned, game.Outcome);
  }

  // Every action endpoint reports an unknown game the same way, through the controller's
  // shared mapping. Covered together so a new action cannot quietly skip it.
  [Theory]
  [InlineData("resume")]
  [InlineData("moves")]
  [InlineData("pass")]
  [InlineData("undo")]
  [InlineData("resign")]
  public async Task ActionOnUnknownGame_ReturnsNotFound(string action)
  {
    using var factory = Factory([]);
    using var client = factory.CreateClient();

    var body = JsonContent.Create(new MoveRequest { X = 0, Y = 0 });
    var response = await client.PostAsync($"/api/games/{Guid.NewGuid()}/{action}", body);

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  // A suggestion whose contents no test depends on. Away from the corner, so a bot move can
  // never collide with a point a test plays itself.
  private static EngineSuggestion Hint() => new(new Domain.Coordinates(5, 5), 0.5);

  // A suggestion telling the bot to pass.
  private static EngineSuggestion PassHint() => new(null, 0.5);

  // Starts a game the human plays as Black, so the bot does not move first, and returns its id.
  private static async Task<Guid> StartGameAsync(HttpClient client)
  {
    StartGameRequest request = new() { PlayerColor = Color.Black, BoardSize = 9, BotStrength = BotStrength.Kyu20 };
    var created = await client.PostAsJsonAsync("/api/games", request);
    return (await ReadGameAsync(created)).Id;
  }

  private static async Task<GameResponse> ReadGameAsync(HttpResponseMessage response)
  {
    GameResponse? game = await response.Content.ReadFromJsonAsync<GameResponse>(_options);
    Assert.NotNull(game);
    return game;
  }

  private static WebApplicationFactory<Program> Factory(
    IReadOnlyList<EngineSuggestion> suggestions,
    FakeGameRepository? repository = null) =>
    new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<GoTrainingPlatformDbContext>();
        services.RemoveAll<IEngineClient>();
        services.RemoveAll<IGameRepository>();
        services.AddSingleton<IEngineClient>(new FakeEngineClient(suggestions));
        services.AddSingleton<IGameRepository>(repository ?? new FakeGameRepository());
      });

      // These tests never reach Postgres or the engine, but the composition root demands
      // both before it will start.
      builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused");
      builder.UseSetting("CurrentPlayer:Id", Guid.NewGuid().ToString());
      builder.UseSetting("Engine:BaseUrl", "http://unused");
    });
}
