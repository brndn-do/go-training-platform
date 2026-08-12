using Engine.Api.Analysis;
using Engine.Api.Processes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KataGoProcessOptions>(
  builder.Configuration.GetSection("KataGo"));

builder.Services.AddSingleton<IKataGoProcessIO, KataGoProcessIO>();
builder.Services.AddSingleton<IKataGoClient, KataGoClient>();

builder.Services.AddScoped<Random>();
builder.Services.AddScoped<SuggestionService>();

var app = builder.Build();

app.Run();
