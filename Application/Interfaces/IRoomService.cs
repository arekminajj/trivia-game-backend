using trivia_game.Application.DTOs;

namespace trivia_game.Application.Interfaces;

public interface IRoomService
{
    Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request);
    JoinRoomResponse JoinRoom(JoinRoomRequest request);
    List<RoomResponse> GetActiveRooms();
}
