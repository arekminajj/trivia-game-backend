using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Mappings;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Exceptions;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Application.Services;

public class RoomService(ITriviaService triviaService, IRoomRepository roomRepository) : IRoomService
{
    private static readonly Random Rng = new();
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";

    public async Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var questions = await triviaService.GetQuestionsAsync(
            request.Amount, request.CategoryId, request.Type);

        var owner = new RoomMember { DisplayName = request.OwnerName };

        var room = new Room
        {
            JoinCode = GenerateCode(),
            Owner = owner,
            Questions = questions,
        };
        room.Members.Add(owner);

        roomRepository.Add(room);
        return RoomMapper.ToRoom(room);
    }

    public JoinRoomResponse JoinRoom(JoinRoomRequest request)
    {
        if (!roomRepository.TryGet(request.JoinCode, out var room))
            throw new RoomNotFoundException(request.JoinCode);

        var member = new RoomMember { DisplayName = request.DisplayName };
        room.Members.Add(member);

        return new JoinRoomResponse(member.Uuid, RoomMapper.ToRoom(room));
    }

    public List<RoomResponse> GetActiveRooms() =>
        roomRepository.GetAll().Select(RoomMapper.ToRoom).ToList();

    private static string GenerateCode(int length = 6) =>
        new(Enumerable.Range(0, length).Select(_ => Chars[Rng.Next(Chars.Length)]).ToArray());
}
