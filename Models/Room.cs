using trivia_game.DTOs;

namespace trivia_game.Models;


public sealed class Room
{
    public string Uuid { get; set; } = String.Empty;
    public string JoinCode { get; set; } = String.Empty;
    public required RoomMember Owner { get; set; }
    public List<RoomMember> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public List<TriviaQuestion?> TriviaQuestions { get; set; } = new();
}