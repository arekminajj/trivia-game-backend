using trivia_game.Domain.Enums;

namespace trivia_game.Domain.Entities;

public class Room
{
    public string Uuid { get; } = Guid.NewGuid().ToString();
    public string JoinCode { get; init; } = string.Empty;
    public RoomMember Owner { get; init; } = default!;
    public List<RoomMember> Members { get; } = new();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public List<TriviaQuestion> Questions { get; init; } = new();
    public string CategoryName { get; init; } = string.Empty;
    public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
    public int CurrentQuestionIndex { get; private set; } = -1;

    // Shuffled once per question so all players see the same answer order
    public List<string> CurrentShuffledAnswers { get; private set; } = new();

    private readonly Dictionary<string, string> _currentRoundAnswers = new();
    private readonly HashSet<string> _readyPlayers = new();
    public IReadOnlyDictionary<string, string> CurrentRoundAnswers => _currentRoundAnswers;

    public TriviaQuestion? CurrentQuestion =>
        CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Questions.Count
            ? Questions[CurrentQuestionIndex]
            : null;

    public bool Start()
    {
        if (Status != RoomStatus.Waiting || Questions.Count == 0)
            return false;

        Status = RoomStatus.InProgress;
        CurrentQuestionIndex = 0;
        ShuffleCurrentAnswers();
        return true;
    }

    public bool SubmitAnswer(string playerUuid, string answer)
    {
        if (Status != RoomStatus.InProgress) return false;
        if (Members.All(m => m.Uuid != playerUuid)) return false;
        // Empty string is the server-side timeout sentinel; any other value must be a valid choice
        if (answer != string.Empty && !CurrentShuffledAnswers.Contains(answer)) return false;
        _currentRoundAnswers[playerUuid] = answer;
        return true;
    }

    public bool AllPlayersAnswered() =>
        Members.Count > 0 && Members.All(m => _currentRoundAnswers.ContainsKey(m.Uuid));

    public void SignalReady(string playerUuid) => _readyPlayers.Add(playerUuid);
    public bool AllPlayersReady() => Members.Count > 0 && Members.All(m => _readyPlayers.Contains(m.Uuid));
    public void ClearReady() => _readyPlayers.Clear();

    public bool RemoveMember(string playerUuid)
    {
        var member = Members.FirstOrDefault(m => m.Uuid == playerUuid);
        if (member is null) return false;
        Members.Remove(member);
        _currentRoundAnswers.Remove(playerUuid);
        _readyPlayers.Remove(playerUuid);
        return true;
    }

    public void ScoreCurrentRound()
    {
        if (CurrentQuestion is null) return;
        foreach (var member in Members)
        {
            if (_currentRoundAnswers.TryGetValue(member.Uuid, out var answer) &&
                answer == CurrentQuestion.CorrectAnswer)
            {
                member.CorrectAnswers++;
                member.Points += PointsFor(CurrentQuestion.Difficulty);
            }
        }
    }

    // Returns false when all questions are exhausted (game over)
    public bool AdvanceQuestion()
    {
        if (Status != RoomStatus.InProgress) return false;
        _currentRoundAnswers.Clear();
        CurrentQuestionIndex++;
        if (CurrentQuestionIndex >= Questions.Count)
        {
            Status = RoomStatus.Finished;
            return false;
        }
        ShuffleCurrentAnswers();
        return true;
    }

    private void ShuffleCurrentAnswers()
    {
        if (CurrentQuestion is null) return;
        var all = CurrentQuestion.IncorrectAnswers.Append(CurrentQuestion.CorrectAnswer).ToList();
        // Seed with room + question index so the order is deterministic for all clients
        var rng = new Random((Uuid + CurrentQuestionIndex).GetHashCode());
        CurrentShuffledAnswers = all.OrderBy(_ => rng.Next()).ToList();
    }

    private static int PointsFor(string difficulty) => difficulty.ToLowerInvariant() switch
    {
        "medium" => 200,
        "hard"   => 300,
        _        => 100
    };
}
