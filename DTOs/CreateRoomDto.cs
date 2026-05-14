
namespace trivia_game.DTOs;

public sealed class CreateRoomDto
{
    public int Amount { get; set; }
    public int Category { get; set; }
    public required string Type { get; set; }
    public required string OwnerName { get; set; }
}