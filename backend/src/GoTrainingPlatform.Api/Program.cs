using System.Text.Json.Serialization;
using GoTrainingPlatform.Api;
using GoTrainingPlatform.Application;
using GoTrainingPlatform.Application.Games;
using GoTrainingPlatform.Application.Orchestration;
using GoTrainingPlatform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// register a fixed current player for development
if (builder.Environment.IsDevelopment())
{
  builder.Services
    .AddOptionsWithValidateOnStart<CurrentPlayerOptions>()
    .Bind(builder.Configuration.GetSection(CurrentPlayerOptions.SectionName))
    .Validate(
      options => options.Id != Guid.Empty,
      "CurrentPlayer__Id must be set to a non-empty GUID.");

  builder.Services.AddSingleton<ICurrentPlayer, DevelopmentCurrentPlayer>();
}

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<GoTrainingPlatformDbContext>(options =>
  options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

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

// Add services to the container.
builder.Services.AddControllers()
  .AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
