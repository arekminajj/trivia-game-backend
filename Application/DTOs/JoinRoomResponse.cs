namespace trivia_game.Application.DTOs;

public sealed record JoinRoomResponse(
    string YourPlayerUuid,
    RoomResponse Room);
