using NSubstitute;
using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Services;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Exceptions;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Tests.Application;

public class RoomServiceTests
{
    private static List<TriviaQuestion> MakeQuestions(int count = 3) =>
        Enumerable.Range(0, count).Select(i => new TriviaQuestion
        {
            Text = $"Q{i}", CorrectAnswer = "A",
            IncorrectAnswers = ["B"], Difficulty = "easy", Category = "Test", Type = "multiple"
        }).ToList();

    // ── CreateRoomAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoomAsync_ReturnsRoomWithOwnerAndCorrectQuestionCount()
    {
        var repo = Substitute.For<IRoomRepository>();
        var trivia = Substitute.For<ITriviaService>();
        trivia.GetQuestionsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>())
              .Returns(Task.FromResult(MakeQuestions(5)));

        var svc = new RoomService(trivia, repo);
        var result = await svc.CreateRoomAsync(new CreateRoomRequest("Alice", 5, null, "multiple"));

        Assert.Equal("Alice", result.Owner.DisplayName);
        Assert.Equal(5, result.TotalQuestions);
        Assert.Single(result.Members); // owner only
    }

    [Fact]
    public async Task CreateRoomAsync_PersistsRoomToRepository()
    {
        var repo = Substitute.For<IRoomRepository>();
        var trivia = Substitute.For<ITriviaService>();
        trivia.GetQuestionsAsync(default, default, default)
              .ReturnsForAnyArgs(Task.FromResult(MakeQuestions()));

        var svc = new RoomService(trivia, repo);
        await svc.CreateRoomAsync(new CreateRoomRequest("Alice", 3, null, null));

        repo.Received(1).Add(Arg.Any<Room>());
    }

    [Fact]
    public async Task CreateRoomAsync_GeneratesSixCharJoinCode()
    {
        var repo = Substitute.For<IRoomRepository>();
        var trivia = Substitute.For<ITriviaService>();
        trivia.GetQuestionsAsync(default, default, default)
              .ReturnsForAnyArgs(Task.FromResult(MakeQuestions()));

        var svc = new RoomService(trivia, repo);
        var result = await svc.CreateRoomAsync(new CreateRoomRequest("Alice", 3, null, null));

        Assert.Equal(6, result.JoinCode.Length);
    }

    // ── JoinRoom ──────────────────────────────────────────────────────────────

    [Fact]
    public void JoinRoom_InvalidCode_ThrowsRoomNotFoundException()
    {
        var repo = Substitute.For<IRoomRepository>();
        Room outRoom = null!;
        repo.TryGet(Arg.Any<string>(), out outRoom).Returns(false);

        var svc = new RoomService(Substitute.For<ITriviaService>(), repo);
        Assert.Throws<RoomNotFoundException>(() =>
            svc.JoinRoom(new JoinRoomRequest("ZZZZZZ", "Bob")));
    }

    [Fact]
    public void JoinRoom_ValidCode_ReturnsMemberUuidAndUpdatedRoom()
    {
        var owner = new RoomMember { DisplayName = "Alice" };
        var room = new Room { JoinCode = "ABCDEF", Owner = owner, Questions = MakeQuestions() };
        room.Members.Add(owner);

        var repo = Substitute.For<IRoomRepository>();
        Room? outParam1 = null;
        repo.TryGet(Arg.Any<string>(), out outParam1).Returns(x => { x[1] = room; return true; });

        var svc = new RoomService(Substitute.For<ITriviaService>(), repo);
        var result = svc.JoinRoom(new JoinRoomRequest("ABCDEF", "Bob"));

        Assert.NotEmpty(result.YourPlayerUuid);
        Assert.Equal(2, result.Room.Members.Count);
        Assert.Contains(result.Room.Members, m => m.DisplayName == "Bob");
    }

    [Fact]
    public void JoinRoom_ValidCode_NewMemberUuidMatchesResponse()
    {
        var owner = new RoomMember { DisplayName = "Alice" };
        var room = new Room { JoinCode = "ABCDEF", Owner = owner, Questions = MakeQuestions() };
        room.Members.Add(owner);

        var repo = Substitute.For<IRoomRepository>();
        Room? outParam2 = null;
        repo.TryGet(Arg.Any<string>(), out outParam2).Returns(x => { x[1] = room; return true; });

        var svc = new RoomService(Substitute.For<ITriviaService>(), repo);
        var result = svc.JoinRoom(new JoinRoomRequest("ABCDEF", "Bob"));

        var bobInRoom = result.Room.Members.First(m => m.DisplayName == "Bob");
        Assert.Equal(result.YourPlayerUuid, bobInRoom.Uuid);
    }
}
