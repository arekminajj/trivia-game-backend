
using System.Collections.Concurrent;
using trivia_game.Models;

namespace trivia_game.Stores;

public class InMemoryConnectionStore : IConnectionStore
{
    private readonly ConcurrentDictionary<string, Connection> _connections = new();

    public void Add(Connection connection)
    {
        _connections[connection.Uuid] = connection;
    }

    public List<Connection> GetAll()
    {
        return _connections.Values.ToList();
    }

    public void Remove(string connectionUuid)
    {
        _connections.TryRemove(connectionUuid, out var removedConnection);
    }

    public bool TryGet(string uuid, out Connection connection)
    {
        return _connections.TryGetValue(uuid, out connection!);
    }
}
