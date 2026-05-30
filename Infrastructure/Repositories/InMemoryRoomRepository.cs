using System.Collections.Concurrent;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Infrastructure.Repositories;

public class InMemoryRoomRepository : IRoomRepository
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    public void Add(Room room) => _rooms[room.JoinCode] = room;

    public bool TryGet(string joinCode, out Room room) =>
        _rooms.TryGetValue(joinCode, out room!);

    public List<Room> GetAll() => _rooms.Values.ToList();

    public void Remove(string joinCode) => _rooms.TryRemove(joinCode, out _);
    public void Save(Room room) { } // in-memory: object is already mutated in place
}
