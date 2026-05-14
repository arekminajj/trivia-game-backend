using trivia_game.Models;

namespace trivia_game.Stores;

//TODO: ADD REDIS STORE IMPLEMENTATION
public interface IRoomStore
{
    void Add(Room room);
    bool TryGet(string code, out Room room);
    List<Room> GetAll();
}