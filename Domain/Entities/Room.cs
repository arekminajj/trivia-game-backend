using System.Text.Json.Serialization;
using trivia_game.Domain.Enums;

namespace trivia_game.Domain.Entities;

public class Room
{
    public string Uuid { get; init; } = Guid.NewGuid().ToString();
    public string JoinCode { get; init; } = string.Empty;
    public RoomMember Owner { get; init; } = default!;
    public List<RoomMember> Members { get; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<TriviaQuestion> Questions { get; init; } = new();
    public string CategoryName { get; set; } = string.Empty;

    [JsonInclude] public RoomStatus Status { get; internal set; } = RoomStatus.Waiting;
    [JsonInclude] public int CurrentQuestionIndex { get; internal set; } = -1;
    [JsonInclude] public List<string> CurrentShuffledAnswers { get; internal set; } = new();
    [JsonInclude] public Dictionary<string, string> CurrentRoundAnswers { get; internal set; } = new();
    [JsonInclude] public HashSet<string> ReadyPlayers { get; internal set; } = new();

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
        if (answer != string.Empty && !CurrentShuffledAnswers.Contains(answer)) return false;
        CurrentRoundAnswers[playerUuid] = answer;
        return true;
    }

    public bool AllPlayersAnswered() =>
        Members.Count > 0 && Members.All(m => CurrentRoundAnswers.ContainsKey(m.Uuid));

    public void SignalReady(string playerUuid) => ReadyPlayers.Add(playerUuid);
    public bool AllPlayersReady() => Members.Count > 0 && Members.All(m => ReadyPlayers.Contains(m.Uuid));
    public void ClearReady() => ReadyPlayers.Clear();

    public bool RemoveMember(string playerUuid)
    {
        var member = Members.FirstOrDefault(m => m.Uuid == playerUuid);
        if (member is null) return false;
        Members.Remove(member);
        CurrentRoundAnswers.Remove(playerUuid);
        ReadyPlayers.Remove(playerUuid);
        return true;
    }

    public void ScoreCurrentRound()
    {
        if (CurrentQuestion is null) return;
        foreach (var member in Members)
        {
            if (CurrentRoundAnswers.TryGetValue(member.Uuid, out var answer) &&
                answer == CurrentQuestion.CorrectAnswer)
            {
                member.CorrectAnswers++;
                member.Points += PointsFor(CurrentQuestion.Difficulty);
            }
        }
    }

    public bool AdvanceQuestion()
    {
        if (Status != RoomStatus.InProgress) return false;
        CurrentRoundAnswers.Clear();
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
        var rng = new Random((Uuid + CurrentQuestionIndex).GetHashCode());
        CurrentShuffledAnswers = all.OrderBy(_ => rng.Next()).ToList();
    }

    public void Restart(List<TriviaQuestion> newQuestions, string newCategoryName)
    {
        Questions.Clear();
        Questions.AddRange(newQuestions);
        CategoryName = newCategoryName;
        Status = RoomStatus.Waiting;
        CurrentQuestionIndex = -1;
        CurrentShuffledAnswers.Clear();
        CurrentRoundAnswers.Clear();
        ReadyPlayers.Clear();
        foreach (var member in Members)
        {
            member.Points = 0;
            member.CorrectAnswers = 0;
        }
    }

    private static int PointsFor(string difficulty) => difficulty.ToLowerInvariant() switch
    {
        "medium" => 200,
        "hard"   => 300,
        _        => 100
    };
}
