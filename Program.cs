using trivia_game.Clients;
using trivia_game.Services;
using trivia_game.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
.AddScoped<ITriviaService, TriviaService>()
.AddScoped<IRoomService, RoomService>()
.AddScoped<ISocketService, SocketsService>();

builder.Services.AddSingleton<IRoomStore, InMemoryRoomStore>();

builder.Services.AddHttpClient<ITriviaClient, TriviaClient>(
);

var app = builder.Build();

app.MapControllers();
app.UseWebSockets();

app.Run();
