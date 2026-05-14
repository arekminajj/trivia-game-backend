
using System.Text.Json;

namespace trivia_game.DTOs;

public sealed class WebSocketsRequestDto
{
    public required string Type { get; set; }
    public JsonElement Payload { get; set; }
}
