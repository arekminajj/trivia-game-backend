namespace trivia_game.Domain.Entities;

public class RoomMember
{
    public string Uuid { get; init; } = Guid.NewGuid().ToString();
    public string DisplayName { get; init; } = string.Empty;
    public int Points { get; set; }
    public int CorrectAnswers { get; set; }
}
