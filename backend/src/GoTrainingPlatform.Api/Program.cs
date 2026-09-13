using System.Text.Json.Serialization;
using GoTrainingPlatform.Api;
using GoTrainingPlatform.Api.ErrorHandling;
using GoTrainingPlatform.Application;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentPlayer, HttpContextCurrentPlayer>();

// Infrastructure
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

// Must come first so it wraps everything downstream.
app.UseExceptionHandler();

app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
