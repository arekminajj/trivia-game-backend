using trivia_game.Services;

namespace trivia_game.WebSockets;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISocketService socketService)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await socketService.HandleConnection(socket);
            return;
        }

        await _next(context);
    }
}