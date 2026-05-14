using trivia_game.DTOs;
using trivia_game.Models;
using trivia_game.Stores;

namespace trivia_game.Services;

public class RoomService : IRoomService
{
    //TODO: MVP solution, may use redis in the future.
    //private Dictionary<String, Room> _activeRooms = new Dictionary<string, Room>();

    private readonly IRoomStore _roomStore;
    private readonly ITriviaService _triviaService;
    public RoomService(
        ITriviaService triviaService,
        IRoomStore roomStore)
    {
        _roomStore = roomStore;
        _triviaService = triviaService;
    }

    public async Task<Room> CreateRoomAsync(
       CreateRoomDto createRoomDto)
    {
        var questions = await _triviaService.GetTriviaQuestionsAsync(
                createRoomDto.Amount,
                createRoomDto.Category,
                createRoomDto.Type);

        var owner = new RoomMember
        {
            CorrectAnswersCount = 0,
            DisplayName = createRoomDto.OwnerName,
            PointsCount = 0,
            Uuid = Guid.NewGuid().ToString()
        };

        var room = new Room
        {
            Uuid = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            TriviaQuestions = questions,
            Members = new List<RoomMember>(){owner},
            JoinCode = GenerateRoomCode(),
            Owner = owner
        };

        _roomStore.Add(room);

        return room; 
    }

    public Room JoinRoom(JoinRoomDto joinRoomDto)
    {
        if (!_roomStore.TryGet(joinRoomDto.JoinCode, out var room))
            throw new Exception("Could not find a room with given join code.");

        var newMember = new RoomMember
        {
            CorrectAnswersCount = 0,
            DisplayName = joinRoomDto.DisplayName,
            PointsCount = 0,
            Uuid = Guid.NewGuid().ToString()
        };

        room.Members.Add(newMember);

        return room;
    }

    public List<Room> GetAllActiveRooms()
    {
        return _roomStore.GetAll();
    }

    private static readonly Random _rand = new Random();
    private const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
    public string GenerateRoomCode(int length = 6)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[_rand.Next(chars.Length)])
            .ToArray());
    }
}