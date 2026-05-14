using trivia_game.Domain.Entities;
using trivia_game.Domain.Enums;

namespace trivia_game.Tests.Domain;

public class RoomTests
{
    private static TriviaQuestion MakeQuestion(string difficulty = "easy") => new()
    {
        Text = "Q?",
        CorrectAnswer = "Correct",
        IncorrectAnswers = ["Wrong1", "Wrong2", "Wrong3"],
        Difficulty = difficulty,
        Category = "Test",
        Type = "multiple"
    };

    private static Room MakeRoom(int members = 2, int questions = 3)
    {
        var playerList = Enumerable.Range(0, members)
            .Select(i => new RoomMember { DisplayName = $"Player{i}" })
            .ToList();

        var room = new Room
        {
            JoinCode = "TESTCD",
            Owner = playerList[0],
            Questions = Enumerable.Range(0, questions).Select(_ => MakeQuestion()).ToList()
        };
        foreach (var p in playerList) room.Members.Add(p);
        return room;
    }

    // ── Start ────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_WhenWaiting_TransitionsToInProgress()
    {
        var room = MakeRoom();
        Assert.True(room.Start());
        Assert.Equal(RoomStatus.InProgress, room.Status);
        Assert.Equal(0, room.CurrentQuestionIndex);
    }

    [Fact]
    public void Start_WhenAlreadyStarted_ReturnsFalse()
    {
        var room = MakeRoom();
        room.Start();
        Assert.False(room.Start());
    }

    [Fact]
    public void Start_WithNoQuestions_ReturnsFalse()
    {
        var owner = new RoomMember { DisplayName = "P" };
        var room = new Room { JoinCode = "X", Owner = owner, Questions = [] };
        room.Members.Add(owner);
        Assert.False(room.Start());
        Assert.Equal(RoomStatus.Waiting, room.Status);
    }

    // ── SubmitAnswer ─────────────────────────────────────────────────────────

    [Fact]
    public void SubmitAnswer_ValidPlayer_RecordsAnswer()
    {
        var room = MakeRoom();
        room.Start();
        Assert.True(room.SubmitAnswer(room.Members[0].Uuid, "Correct"));
        Assert.Equal("Correct", room.CurrentRoundAnswers[room.Members[0].Uuid]);
    }

    [Fact]
    public void SubmitAnswer_GameNotStarted_ReturnsFalse()
    {
        var room = MakeRoom();
        Assert.False(room.SubmitAnswer(room.Members[0].Uuid, "Correct"));
    }

    [Fact]
    public void SubmitAnswer_UnknownPlayer_ReturnsFalse()
    {
        var room = MakeRoom();
        room.Start();
        Assert.False(room.SubmitAnswer("unknown-uuid", "Correct"));
    }

    // ── AllPlayersAnswered ───────────────────────────────────────────────────

    [Fact]
    public void AllPlayersAnswered_WhenAllAnswered_ReturnsTrue()
    {
        var room = MakeRoom(members: 2);
        room.Start();
        room.SubmitAnswer(room.Members[0].Uuid, "Correct");
        room.SubmitAnswer(room.Members[1].Uuid, "Wrong1");
        Assert.True(room.AllPlayersAnswered());
    }

    [Fact]
    public void AllPlayersAnswered_WhenPartiallyAnswered_ReturnsFalse()
    {
        var room = MakeRoom(members: 2);
        room.Start();
        room.SubmitAnswer(room.Members[0].Uuid, "Correct");
        Assert.False(room.AllPlayersAnswered());
    }

    // ── ScoreCurrentRound ────────────────────────────────────────────────────

    [Fact]
    public void ScoreCurrentRound_CorrectAnswer_AwardsPoints()
    {
        var room = MakeRoom(members: 2);
        room.Start();
        room.SubmitAnswer(room.Members[0].Uuid, "Correct");
        room.SubmitAnswer(room.Members[1].Uuid, "Wrong1");
        room.ScoreCurrentRound();

        Assert.Equal(100, room.Members[0].Points);
        Assert.Equal(1, room.Members[0].CorrectAnswers);
        Assert.Equal(0, room.Members[1].Points);
        Assert.Equal(0, room.Members[1].CorrectAnswers);
    }

    [Theory]
    [InlineData("easy",   100)]
    [InlineData("medium", 200)]
    [InlineData("hard",   300)]
    public void ScoreCurrentRound_DifficultyMultiplier_AwardsCorrectPoints(string difficulty, int expected)
    {
        var player = new RoomMember { DisplayName = "P" };
        var room = new Room
        {
            JoinCode = "X",
            Owner = player,
            Questions = [MakeQuestion(difficulty)]
        };
        room.Members.Add(player);
        room.Start();
        room.SubmitAnswer(player.Uuid, "Correct");
        room.ScoreCurrentRound();

        Assert.Equal(expected, player.Points);
    }

    // ── AdvanceQuestion ──────────────────────────────────────────────────────

    [Fact]
    public void AdvanceQuestion_MoreQuestionsRemain_ReturnsTrueAndIncrementsIndex()
    {
        var room = MakeRoom(questions: 3);
        room.Start();
        foreach (var m in room.Members) room.SubmitAnswer(m.Uuid, "x");
        room.ScoreCurrentRound();

        Assert.True(room.AdvanceQuestion());
        Assert.Equal(1, room.CurrentQuestionIndex);
        Assert.Equal(RoomStatus.InProgress, room.Status);
    }

    [Fact]
    public void AdvanceQuestion_LastQuestion_ReturnsFalseAndSetsFinished()
    {
        var room = MakeRoom(questions: 1);
        room.Start();
        foreach (var m in room.Members) room.SubmitAnswer(m.Uuid, "x");
        room.ScoreCurrentRound();

        Assert.False(room.AdvanceQuestion());
        Assert.Equal(RoomStatus.Finished, room.Status);
    }

    [Fact]
    public void AdvanceQuestion_ClearsRoundAnswers()
    {
        var room = MakeRoom(questions: 2);
        room.Start();
        foreach (var m in room.Members) room.SubmitAnswer(m.Uuid, "x");
        room.ScoreCurrentRound();
        room.AdvanceQuestion();

        Assert.Empty(room.CurrentRoundAnswers);
    }
}
