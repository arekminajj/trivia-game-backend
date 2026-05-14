
namespace trivia_game.DTOs;

public sealed class JoinRoomDto
{
    public required string JoinCode { get; set; }
    public required string DisplayName { get; set; }
}
