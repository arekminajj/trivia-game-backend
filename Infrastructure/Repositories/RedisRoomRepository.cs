using System.Text.Json;
using StackExchange.Redis;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Infrastructure.Repositories;

public class RedisRoomRepository(IConnectionMultiplexer redis) : IRoomRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(4);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
    };

    private IDatabase Db => redis.GetDatabase();
    private static string RoomKey(string joinCode) => $"room:{joinCode}";
    private const string RoomsIndexKey = "rooms:all";

    public void Add(Room room)
    {
        var json = JsonSerializer.Serialize(room, JsonOptions);
        Db.StringSet(RoomKey(room.JoinCode), json, Ttl);
        Db.SetAdd(RoomsIndexKey, room.JoinCode);
    }

    public void Save(Room room)
    {
        var json = JsonSerializer.Serialize(room, JsonOptions);
        // Preserve remaining TTL — fetch it first, fall back to full TTL if key is new
        var remaining = Db.KeyTimeToLive(RoomKey(room.JoinCode));
        Db.StringSet(RoomKey(room.JoinCode), json, remaining ?? Ttl);
    }

    public bool TryGet(string joinCode, out Room room)
    {
        var json = Db.StringGet(RoomKey(joinCode));
        if (json.IsNullOrEmpty)
        {
            room = null!;
            return false;
        }
        room = JsonSerializer.Deserialize<Room>((string)json!, JsonOptions)!;
        return true;
    }

    public List<Room> GetAll()
    {
        var codes = Db.SetMembers(RoomsIndexKey);
        var rooms = new List<Room>();
        foreach (var code in codes)
        {
            if (TryGet(code!, out var room))
                rooms.Add(room);
            else
                Db.SetRemove(RoomsIndexKey, code); // clean up expired entries
        }
        return rooms;
    }

    public void Remove(string joinCode)
    {
        Db.KeyDelete(RoomKey(joinCode));
        Db.SetRemove(RoomsIndexKey, joinCode);
    }
}
