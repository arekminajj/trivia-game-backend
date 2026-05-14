namespace trivia_game.Application.DTOs;

public sealed record CreateRoomRequest(
    string OwnerName,
    int? Amount,
    int? CategoryId,
    string? Type);
