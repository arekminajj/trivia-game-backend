using NSubstitute;
using trivia_game.Application.DTOs;
using trivia_game.Application.Services;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Tests.Application;

public class GameServiceTests
{
    private static TriviaQuestion MakeQuestion(string difficulty = "easy") => new()
    {
        Text = "Q?",
        CorrectAnswer = "Correct",
        IncorrectAnswers = ["Wrong"],
        Difficulty = difficulty,
        Category = "Test",
        Type = "multiple"
    };

    private static Room MakeStartedRoom(int questions = 2)
    {
        var owner = new RoomMember { DisplayName = "Alice" };
        var other = new RoomMember { DisplayName = "Bob" };
        var room = new Room
        {
            JoinCode = "ABCDEF",
            Owner = owner,
            Questions = Enumerable.Range(0, questions).Select(_ => MakeQuestion()).ToList()
        };
        room.Members.Add(owner);
        room.Members.Add(other);
        room.Start();
        return room;
    }

    private static (GameService svc, IRoomRepository repo) Setup(Room? room = null)
    {
        var repo = Substitute.For<IRoomRepository>();
        if (room is not null)
        {
            Room? outParam = null;
            repo.TryGet(Arg.Any<string>(), out outParam).Returns(x => { x[1] = room; return true; });
        }
        return (new GameService(repo), repo);
    }

    // ── ConnectToRoom ─────────────────────────────────────────────────────────

    [Fact]
    public void ConnectToRoom_RoomNotFound_ReturnsFailure()
    {
        var (svc, _) = Setup();
        Assert.False(svc.ConnectToRoom("ZZZZZZ", "any").Success);
    }

    [Fact]
    public void ConnectToRoom_PlayerNotInRoom_ReturnsFailure()
    {
        var room = MakeStartedRoom();
        var (svc, _) = Setup(room);
        Assert.False(svc.ConnectToRoom(room.JoinCode, "unknown-uuid").Success);
    }

    [Fact]
    public void ConnectToRoom_ValidPlayer_ReturnsRoomAndPlayer()
    {
        var room = MakeStartedRoom();
        var (svc, _) = Setup(room);
        var result = svc.ConnectToRoom(room.JoinCode, room.Owner.Uuid);
        Assert.True(result.Success);
        Assert.Equal(room.Owner.Uuid, result.Player!.Uuid);
        Assert.Equal(room.JoinCode, result.Room!.JoinCode);
    }

    // ── StartGame ─────────────────────────────────────────────────────────────

    [Fact]
    public void StartGame_RoomNotFound_ReturnsFailure()
    {
        var (svc, _) = Setup();
        Assert.False(svc.StartGame("ZZZZZZ", "any").Success);
    }

    [Fact]
    public void StartGame_NonOwner_ReturnsFailure()
    {
        var owner = new RoomMember { DisplayName = "Alice" };
        var other = new RoomMember { DisplayName = "Bob" };
        var room = new Room { JoinCode = "ABCDEF", Owner = owner, Questions = [MakeQuestion()] };
        room.Members.Add(owner);
        room.Members.Add(other);
        var (svc, _) = Setup(room);

        Assert.False(svc.StartGame(room.JoinCode, other.Uuid).Success);
    }

    [Fact]
    public void StartGame_ValidOwner_ReturnsFirstQuestion()
    {
        var owner = new RoomMember { DisplayName = "Alice" };
        var room = new Room { JoinCode = "ABCDEF", Owner = owner, Questions = [MakeQuestion()] };
        room.Members.Add(owner);
        var (svc, _) = Setup(room);

        var result = svc.StartGame(room.JoinCode, owner.Uuid);
        Assert.True(result.Success);
        Assert.NotNull(result.FirstQuestion);
        Assert.Equal(0, result.FirstQuestion!.Index);
        Assert.NotEmpty(result.FirstQuestion.Answers);
    }

    // ── SubmitAnswer ──────────────────────────────────────────────────────────

    [Fact]
    public void SubmitAnswer_NotLastPlayer_ReturnsAccepted()
    {
        var room = MakeStartedRoom(questions: 2);
        var (svc, _) = Setup(room);

        var result = svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "x");
        Assert.True(result.Success);
        Assert.Equal(SubmitAnswerOutcome.Accepted, result.Outcome);
    }

    [Fact]
    public void SubmitAnswer_AllAnswered_MoreQuestions_ReturnsRoundComplete()
    {
        var room = MakeStartedRoom(questions: 2);
        var (svc, _) = Setup(room);
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "Correct");

        var result = svc.SubmitAnswer(room.JoinCode, room.Members[1].Uuid, "Wrong");
        Assert.Equal(SubmitAnswerOutcome.RoundComplete, result.Outcome);
        Assert.NotNull(result.RoundResult);
        Assert.Equal("Correct", result.RoundResult!.CorrectAnswer);
        Assert.NotNull(result.NextQuestion);
    }

    [Fact]
    public void SubmitAnswer_AllAnswered_LastQuestion_ReturnsGameOver()
    {
        var room = MakeStartedRoom(questions: 1);
        var (svc, _) = Setup(room);
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "Correct");

        var result = svc.SubmitAnswer(room.JoinCode, room.Members[1].Uuid, "Wrong");
        Assert.Equal(SubmitAnswerOutcome.GameOver, result.Outcome);
        Assert.NotNull(result.FinalLeaderboard);
        Assert.Null(result.NextQuestion);
    }

    [Fact]
    public void SubmitAnswer_CorrectAnswer_ReflectedInLeaderboard()
    {
        var room = MakeStartedRoom(questions: 1);
        var (svc, _) = Setup(room);
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "Correct"); // Alice correct
        var result = svc.SubmitAnswer(room.JoinCode, room.Members[1].Uuid, "Wrong");   // Bob wrong

        var alice = result.FinalLeaderboard!.First(p => p.Uuid == room.Members[0].Uuid);
        var bob   = result.FinalLeaderboard!.First(p => p.Uuid == room.Members[1].Uuid);
        Assert.Equal(100, alice.Points);
        Assert.Equal(0, bob.Points);
    }

    [Fact]
    public void SubmitAnswer_LeaderboardSortedByPointsDescending()
    {
        var room = MakeStartedRoom(questions: 1);
        var (svc, _) = Setup(room);
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "Correct");
        var result = svc.SubmitAnswer(room.JoinCode, room.Members[1].Uuid, "Wrong");

        var lb = result.FinalLeaderboard!;
        Assert.True(lb[0].Points >= lb[1].Points);
    }

    // ── TimeOutRound ──────────────────────────────────────────────────────────

    [Fact]
    public void TimeOutRound_WrongQuestionIndex_ReturnsFailure()
    {
        var room = MakeStartedRoom();
        var (svc, _) = Setup(room);
        Assert.False(svc.TimeOutRound(room.JoinCode, questionIndex: 99).Success);
    }

    [Fact]
    public void TimeOutRound_SubmitsEmptyForUnansweredAndFinalizes()
    {
        var room = MakeStartedRoom(questions: 2);
        var (svc, _) = Setup(room);
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "Correct"); // only one answered

        var result = svc.TimeOutRound(room.JoinCode, questionIndex: 0);
        Assert.True(result.Success);
        Assert.NotNull(result.RoundResult);
    }

    [Fact]
    public void TimeOutRound_AfterRoundAlreadyAdvanced_ReturnsFailure()
    {
        var room = MakeStartedRoom(questions: 2);
        var (svc, _) = Setup(room);
        // Complete Q0 naturally
        svc.SubmitAnswer(room.JoinCode, room.Members[0].Uuid, "x");
        svc.SubmitAnswer(room.JoinCode, room.Members[1].Uuid, "x");
        // Timer fires late for Q0 — should be rejected (now on Q1)
        Assert.False(svc.TimeOutRound(room.JoinCode, questionIndex: 0).Success);
    }
}
