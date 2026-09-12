using GoTrainingPlatform.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// Hosts the API against a real, throwaway Postgres for the lifetime of a test collection.
/// Unlike the fake-everything factory the games endpoints use, this one keeps the real
/// <see cref="GoTrainingPlatformDbContext"/>, so Identity's user store actually works.
/// </summary>
public sealed class PostgresApiFixture : IAsyncLifetime
{
  private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

  private WebApplicationFactory<Program>? _factory;

  private WebApplicationFactory<Program> Factory =>
    _factory ?? throw new InvalidOperationException("The fixture was used before InitializeAsync ran.");

  /// <inheritdoc/>
  public async Task InitializeAsync()
  {
    await _container.StartAsync();

    _factory = new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());

        // The composition root demands both before it will start; no test here reaches them.
        builder.UseSetting("CurrentPlayer:Id", Guid.NewGuid().ToString());
        builder.UseSetting("Engine:BaseUrl", "http://unused");
      });

    await using var context = CreateContext();
    await context.Database.MigrateAsync();
  }

  /// <inheritdoc/>
  public async Task DisposeAsync()
  {
    if (_factory is not null)
    {
      await _factory.DisposeAsync();
    }

    await _container.DisposeAsync();
  }

  /// <summary>
  /// Creates a client whose base address is HTTPS. The session cookie is written with
  /// <c>Secure</c>, and <see cref="System.Net.CookieContainer"/> will not send such a cookie
  /// back over an <c>http</c> address — so an http client silently loses the session after
  /// signing in. There is no TLS behind the test server; only the scheme matters.
  /// </summary>
  /// <returns>A client that keeps cookies across requests.</returns>
  public HttpClient CreateClient() =>
    Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost"),
      HandleCookies = true,
    });

  /// <summary>
  /// Creates a client that discards cookies, for asserting on <c>Set-Cookie</c> directly
  /// rather than following a session.
  /// </summary>
  /// <returns>A client that does not keep cookies.</returns>
  public HttpClient CreateCookielessClient() =>
    Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost"),
      HandleCookies = false,
    });

  /// <summary>
  /// Creates a context pointed at this fixture's container, for asserting on persisted state.
  /// Callers get a fresh context per call.
  /// </summary>
  /// <returns>The database context.</returns>
  public GoTrainingPlatformDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<GoTrainingPlatformDbContext>()
      .UseNpgsql(_container.GetConnectionString())
      .UseSnakeCaseNamingConvention()
      .Options;

    return new GoTrainingPlatformDbContext(options);
  }
}
