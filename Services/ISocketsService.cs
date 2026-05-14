

using System.Net.WebSockets;

namespace trivia_game.Services;

public interface ISocketService
{
    public Task HandleConnection(WebSocket webSocket);
}