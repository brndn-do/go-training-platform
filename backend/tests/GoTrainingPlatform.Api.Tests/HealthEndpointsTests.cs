using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// The health endpoints must answer without any external dependency. This host is given a
/// connection string that points nowhere and an engine that does not exist, so a probe that
/// quietly grew a dependency fails here instead of in production.
/// </summary>
public sealed class HealthEndpointsTests : IDisposable
{
  private const string StartupPath = "/health/startup";
  private const string ReadyPath = "/health/ready";
  private const string LivePath = "/health/live";
  private const string UnreachableDatabase =
    "Host=127.0.0.1;Port=1;Database=nonexistent;Username=none;Password=none";

  private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
      builder.UseSetting("Cors:AllowedOrigins:0", "https://app.example.com");
      builder.UseSetting("ConnectionStrings:DefaultConnection", UnreachableDatabase);
      builder.UseSetting("Engine:BaseUrl", "http://unused");
    });

  [Theory]
  [InlineData(StartupPath)]
  [InlineData(ReadyPath)]
  [InlineData(LivePath)]
  public async Task Get_NoExternalDependencyAvailable_ReturnsHealthy(string path)
  {
    var client = _factory.CreateClient();

    var response = await client.GetAsync(path);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
  }

  /// <inheritdoc/>
  public void Dispose() => _factory.Dispose();
}
