using trivia_game.Domain.Entities;

namespace trivia_game.Domain.Interfaces.Repositories;

public interface IRoomRepository
{
    void Add(Room room);
    bool TryGet(string joinCode, out Room room);
    List<Room> GetAll();
    void Remove(string joinCode);
    void Save(Room room);
}
