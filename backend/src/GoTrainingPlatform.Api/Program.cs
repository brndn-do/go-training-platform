using System.Text.Json.Serialization;
using Azure.Identity;
using GoTrainingPlatform.Api;
using GoTrainingPlatform.Api.Endpoints;
using GoTrainingPlatform.Api.ErrorHandling;
using GoTrainingPlatform.Application;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddOptionsWithValidateOnStart<CorsOptions>()
  .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
  .Validate(
    options =>
      options.AllowedOrigins.Length > 0 // at least one allowed origin
        && options.AllowedOrigins.All(
          origin =>
            origin.Length > 0
            && origin.Last() != '/' // no trailing slash
            && Uri.TryCreate(origin, UriKind.Absolute, out Uri? uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)),
    "Cors__AllowedOrigins must be set to at least one valid http/https origin.");

CorsOptions corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
  ?? throw new InvalidOperationException("Cors__AllowedOrigins must be set to at least one valid http/https origin.");

builder.Services.AddCors((options) =>
{
  options.AddPolicy("Spa", policy =>
  {
    policy.WithOrigins(corsOptions.AllowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<GoTrainingPlatformDbContext>(options =>
  options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

// Identity. Password and lockout rules follow NIST SP 800-63B
builder.Services
  .AddIdentityCore<ApplicationUser>(options =>
  {
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 100;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
  })
  .AddEntityFrameworkStores<GoTrainingPlatformDbContext>()
  .AddSignInManager();

builder.Services
  .AddAuthentication(IdentityConstants.ApplicationScheme)
  .AddIdentityCookies();

// ADR 29: the session rides in this cookie, first-party to the API's own hostname.
builder.Services.ConfigureApplicationCookie(options =>
{
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.Cookie.SameSite = SameSiteMode.Lax;

  // Identity's handler redirects to a login page by default. An API answers with a status.
  options.Events.OnRedirectToLogin = context =>
  {
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    return Task.CompletedTask;
  };

  options.Events.OnRedirectToAccessDenied = context =>
  {
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    return Task.CompletedTask;
  };
});

// Data protection. The session cookie is sealed with this key ring, so the ring has to outlive
// any one container and be shared by every instance (ADR 32).
//
// Validated inline rather than with ValidateOnStart, because the key ring is configured during
// service registration, below, which runs before start-time validation could report anything.
// A missing section binds to no Provider, which the first check rejects along with an
// out-of-range one; only a misspelled name fails earlier still, while the section is bound.
var dataProtection = builder.Configuration.GetSection(KeyRingOptions.SectionName).Get<KeyRingOptions>()
  ?? new KeyRingOptions();

if (!Enum.IsDefined(dataProtection.Provider))
{
  throw new InvalidOperationException("DataProtection__Provider must be set to AzureBlob, FileSystem or Ephemeral.");
}

if (string.IsNullOrWhiteSpace(dataProtection.ApplicationName))
{
  throw new InvalidOperationException("DataProtection__ApplicationName must be set.");
}

var keyRing = builder.Services.AddDataProtection().SetApplicationName(dataProtection.ApplicationName);

switch (dataProtection.Provider)
{
  case KeyRingProvider.AzureBlob:
    {
      var credential = new DefaultAzureCredential();

      keyRing
        .PersistKeysToAzureBlobStorage(RequireHttpsUri(dataProtection.BlobUri, "DataProtection__BlobUri"), credential)
        .ProtectKeysWithAzureKeyVault(RequireHttpsUri(dataProtection.KeyVaultKeyUri, "DataProtection__KeyVaultKeyUri"), credential);
      break;
    }

  case KeyRingProvider.FileSystem:
    {
      if (string.IsNullOrWhiteSpace(dataProtection.KeyRingPath))
      {
        throw new InvalidOperationException(
          "DataProtection__KeyRingPath must be set when DataProtection__Provider is FileSystem.");
      }

      keyRing.PersistKeysToFileSystem(new DirectoryInfo(dataProtection.KeyRingPath));
      break;
    }

  case KeyRingProvider.Ephemeral:
    // Replaces the provider outright, so the application name set above stops applying. Nothing
    // shares an ephemeral key ring, so there is no one to be isolated from.
    keyRing.UseEphemeralDataProtectionProvider();
    break;

  default:
    // Unreachable today. Left loud so a mode added to the enum without a case here fails to
    // boot instead of silently falling back to the framework's default key storage.
    throw new InvalidOperationException($"DataProtection__Provider {dataProtection.Provider} is not handled.");
}

// helper
static Uri RequireHttpsUri(string? value, string setting) =>
  Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps
    ? uri
    : throw new InvalidOperationException($"{setting} must be set to an absolute https URI.");

builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentPlayer, HttpContextCurrentPlayer>();

// Infrastructure
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IGameRepository, GameRepository>();

// Engine
builder.Services.
  AddOptionsWithValidateOnStart<EngineOptions>()
  .Bind(builder.Configuration.GetSection(EngineOptions.SectionName))
  .Validate(
    options =>
      Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps),
    "Engine__BaseUrl must be set to a valid http/https URL.");

builder.Services.AddHttpClient<IEngineClient, EngineClient>((sp, client) =>
{
  var engine = sp.GetRequiredService<IOptions<EngineOptions>>().Value;
  client.BaseAddress = new Uri(engine.BaseUrl);
});

// Application
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<TurnOrchestrator>();

builder.Host.UseDefaultServiceProvider((context, options) =>
{
  options.ValidateScopes = true;
  options.ValidateOnBuild = true;
});

// Error handling. AddProblemDetails supplies the RFC 9457 body (and its traceId) that the
// handler writes, and that the framework falls back to for anything the handler declines.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();

builder.Services.AddControllers()
  .AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// The key ring is opened lazily, so a key store the app cannot reach would boot healthy and
// fail on the first login. Protecting a throwaway payload forces it open now.
app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("startup probe").Protect("probe");

// Must come first so it wraps everything downstream.
app.UseExceptionHandler();

app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapHealthEndpoints();

app.Run();
