namespace trivia_game.Application.DTOs;

public enum SubmitAnswerOutcome
{
    Accepted,
    RoundComplete,
    GameOver
}

public enum DisconnectOutcome
{
    PlayerRemoved,
    RoundComplete,
    GameOver,
    RoomEmpty,
    ReadyPhaseComplete,
    RoomClosedByHost
}

public sealed record SignalReadyResult(
    bool Success,
    string? Error = null,
    bool AllReady = false,
    QuestionResponse? NextQuestion = null,
    List<PlayerResponse>? FinalLeaderboard = null);

public sealed record ConnectToRoomResult(
    bool Success,
    string? Error = null,
    PlayerResponse? Player = null,
    RoomResponse? Room = null);

public sealed record StartGameResult(
    bool Success,
    string? Error = null,
    QuestionResponse? FirstQuestion = null);

public sealed record SubmitAnswerResult(
    bool Success,
    string? Error = null,
    SubmitAnswerOutcome? Outcome = null,
    RoundResultResponse? RoundResult = null,
    QuestionResponse? NextQuestion = null,
    List<PlayerResponse>? FinalLeaderboard = null);

public sealed record DisconnectFromRoomResult(
    bool Success,
    string? Error = null,
    DisconnectOutcome? Outcome = null,
    string? RoomCode = null,
    RoundResultResponse? RoundResult = null,
    QuestionResponse? NextQuestion = null,
    List<PlayerResponse>? FinalLeaderboard = null);
