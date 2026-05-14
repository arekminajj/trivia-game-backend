using trivia_game.Models;

namespace trivia_game.Stores;

public interface IConnectionStore
{
    void Add(Connection connection);
    void Remove(string connectionUuid); 
    bool TryGet(string uuid, out Connection connection);
    List<Connection> GetAll();
}
