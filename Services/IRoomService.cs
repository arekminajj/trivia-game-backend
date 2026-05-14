using trivia_game.DTOs;
using trivia_game.Models;

namespace trivia_game.Services;

public interface IRoomService
{
    public Task<Room> CreateRoomAsync(
        CreateRoomDto createRoomDto);
    public Room JoinRoom(JoinRoomDto joinRoomDto);
    public List<Room> GetAllActiveRooms();
    public string GenerateRoomCode(int length = 6);
}