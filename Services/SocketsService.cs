
using System.Net.WebSockets;
using System.Text;
using System.Text.Unicode;

namespace trivia_game.Services;

public class SocketsService() : ISocketService
{
    public async Task HandleConnection(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);


        while (!receiveResult.CloseStatus.HasValue)
        {
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                var messageJson = Encoding.UTF32.GetString(buffer, 0, receiveResult.Count)
            }

            await SocketRespond(webSocket);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}