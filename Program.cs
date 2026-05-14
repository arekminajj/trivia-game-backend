using Scalar.AspNetCore;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Options;
using trivia_game.Application.Services;
using trivia_game.Domain.Interfaces.Providers;
using trivia_game.Domain.Interfaces.Repositories;
using trivia_game.Infrastructure.ExternalApis.OpenTdb;
using trivia_game.Infrastructure.Repositories;
using trivia_game.Presentation.Hubs;
using trivia_game.Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

builder.Services.Configure<GameOptions>(builder.Configuration.GetSection("Game"));

builder.Services
    .AddScoped<IRoomService, RoomService>()
    .AddScoped<ITriviaService, TriviaService>()
    .AddSingleton<IGameService, GameService>()
    .AddSingleton<IGameTimerService, GameTimerService>();

builder.Services.AddSingleton<IRoomRepository, InMemoryRoomRepository>();

builder.Services.AddHttpClient<ITriviaProvider, OpenTdbClient>(client =>
    client.BaseAddress = new Uri("https://opentdb.com/"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.Title = "Trivia Game API");
}

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();
