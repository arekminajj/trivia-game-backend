using System.Net.WebSockets;

namespace trivia_game.Models;

public class Connection
{
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
    public WebSocket Socket { get; set; } = default!;

    public string? RoomCode { get; set; }
    public string? UserUuid { get; set; }
}