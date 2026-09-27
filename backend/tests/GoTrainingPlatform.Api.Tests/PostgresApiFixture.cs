using System.Net;
using System.Net.Http.Json;
using GoTrainingPlatform.Api.Contracts;
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
  /// <summary>
  /// The first origin the host allows to call it with credentials.
  /// </summary>
  public const string AllowedOrigin = "http://localhost:5173";

  /// <summary>
  /// The second origin the host allows to call it with credentials.
  /// </summary>
  public const string SecondAllowedOrigin = "https://app.example.com";

  /// <summary>
  /// The application name the fixture's own host pins. Subkeys are derived from it, so a host
  /// built with a different one cannot read the first one's cookies.
  /// </summary>
  public const string DefaultApplicationName = "PostgresApiFixture";

  private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

  private WebApplicationFactory<Program>? _factory;

  private WebApplicationFactory<Program> Factory =>
    _factory ?? throw new InvalidOperationException("The fixture was used before InitializeAsync ran.");

  /// <inheritdoc/>
  public async Task InitializeAsync()
  {
    await _container.StartAsync();

    _factory = BuildFactory(DefaultApplicationName, keyRingPath: null);

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
  /// Registers a new account and signs it in.
  /// </summary>
  /// <returns>A client carrying that account's session cookie.</returns>
  /// <exception cref="InvalidOperationException">Registering or signing in did not succeed.</exception>
  public async Task<HttpClient> CreateSignedInClientAsync()
  {
    string email = $"{Guid.NewGuid():N}@example.com";
    const string password = "correct horse battery staple";
    var client = CreateClient();

    var registered = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password });
    var signedIn = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    if (registered.StatusCode != HttpStatusCode.NoContent || signedIn.StatusCode != HttpStatusCode.NoContent)
    {
      throw new InvalidOperationException(
        $"Test setup failed: register returned {registered.StatusCode}, login returned {signedIn.StatusCode}.");
    }

    return client;
  }

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

  /// <summary>
  /// Builds an additional host over this fixture's database, independent of the fixture's own.
  /// Use it to observe what a second or replacement host makes of a cookie the first one issued.
  /// The caller owns the result and must dispose it.
  /// </summary>
  /// <param name="keyRingPath">
  /// A directory to persist the data protection key ring to. Two hosts given the same directory
  /// share a key ring; given different directories, they share nothing. Pass <c>null</c> to keep
  /// the keys with the host, so they die with it.
  /// </param>
  /// <param name="applicationName">
  /// The application name to pin, defaulting to <see cref="DefaultApplicationName"/> so a host
  /// sharing a key ring with the fixture's own also shares its derived subkeys.
  /// </param>
  /// <returns>A host that has not been started; the first client request starts it.</returns>
  public WebApplicationFactory<Program> CreateSeparateHost(
    string? keyRingPath = null,
    string applicationName = DefaultApplicationName) =>
    BuildFactory(applicationName, keyRingPath);

  private WebApplicationFactory<Program> BuildFactory(string applicationName, string? keyRingPath) =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting("Cors:AllowedOrigins:1", SecondAllowedOrigin);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());

        // The composition root demands it before it will start. It points nowhere, so any
        // request that reaches the engine fails as unavailable.
        builder.UseSetting("Engine:BaseUrl", "http://unused");

        builder.UseSetting("DataProtection:ApplicationName", applicationName);
        builder.UseSetting("DataProtection:Provider", keyRingPath is null ? "Ephemeral" : "FileSystem");

        if (keyRingPath is not null)
        {
          builder.UseSetting("DataProtection:KeyRingPath", keyRingPath);
        }
      });
}
