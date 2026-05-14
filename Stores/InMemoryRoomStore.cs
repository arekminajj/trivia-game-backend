using System.Collections.Concurrent;
using trivia_game.Models;

namespace trivia_game.Stores;

public class InMemoryRoomStore : IRoomStore
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    public void Add(Room room)
    {
        _rooms[room.JoinCode] = room;
    }

    public bool TryGet(string code, out Room room)
    {
        return _rooms.TryGetValue(code, out room!);
    }

    public List<Room> GetAll()
    {
        return _rooms.Values.ToList();
    }
}