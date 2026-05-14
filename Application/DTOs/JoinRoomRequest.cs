namespace trivia_game.Application.DTOs;

public sealed record JoinRoomRequest(
    string JoinCode,
    string DisplayName);
