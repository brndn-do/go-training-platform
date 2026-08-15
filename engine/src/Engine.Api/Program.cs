using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Processes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KataGoProcessOptions>(
  builder.Configuration.GetSection("KataGoProcess"));
builder.Services.Configure<KataGoProcessIOOptions>(
  builder.Configuration.GetSection("KataGoProcessIO"));
builder.Services.Configure<KataGoClientOptions>(
  builder.Configuration.GetSection("KataGoClient"));
builder.Services.Configure<ReadyHealthCheckOptions>(
  builder.Configuration.GetSection("ReadyHealthCheck"));
builder.Services.Configure<LiveHealthCheckOptions>(
  builder.Configuration.GetSection("LiveHealthCheck"));

builder.Services.AddSingleton<IKataGoProcessIO, KataGoProcessIO>();
builder.Services.AddSingleton<IKataGoClient, KataGoClient>();

builder.Services.AddScoped<Random>();
builder.Services.AddScoped<SuggestionService>();

builder.Services.AddHealthChecks()
  .AddCheck<StartupHealthCheck>("startup", tags: ["startup"])
  .AddCheck<ReadyHealthCheck>("ready", tags: ["ready"])
  .AddCheck<LiveHealthCheck>("live", tags: ["live"]);

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSuggestionEndpoints();
app.MapWarmUpEndpoints();

app.Run();
