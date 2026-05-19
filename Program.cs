using Microsoft.AspNetCore.Diagnostics;
using Polly;
using Scalar.AspNetCore;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Options;
using trivia_game.Application.Services;
using trivia_game.Domain.Exceptions;
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
    client.BaseAddress = new Uri("https://opentdb.com/"))
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, message) = error switch
    {
        ExternalApiUnavailableException ex => (503, ex.Message),
        ArgumentException ex               => (400, ex.Message),
        RoomNotFoundException ex           => (404, ex.Message),
        InvalidGameOperationException ex   => (422, ex.Message),
        _                                  => (500, "An unexpected error occurred.")
    };
    context.Response.StatusCode = status;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = message });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.Title = "Trivia Game API");
}

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();
