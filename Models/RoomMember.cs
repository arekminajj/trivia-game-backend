
namespace trivia_game.Models;

public sealed class RoomMember
{
    public string Uuid { get; set; } = String.Empty;
    public string DisplayName { get; set; } = String.Empty;
    public uint PointsCount { get; set; }
    public int CorrectAnswersCount { get; set; }
}