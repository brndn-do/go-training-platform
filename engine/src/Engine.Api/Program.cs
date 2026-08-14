using Engine.Api.Analysis;
using Engine.Api.Endpoints;
using Engine.Api.Processes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KataGoProcessOptions>(
  builder.Configuration.GetSection("KataGo"));

builder.Services.AddSingleton<IKataGoProcessIO, KataGoProcessIO>();
builder.Services.AddSingleton<IKataGoClient, KataGoClient>();

builder.Services.AddScoped<Random>();
builder.Services.AddScoped<SuggestionService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSuggestionEndpoints();
app.MapWarmUpEndpoints();

app.Run();
