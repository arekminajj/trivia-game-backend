namespace trivia_game.Application.DTOs;

public enum SubmitAnswerOutcome
{
    Accepted,      // answer recorded, waiting for other players
    RoundComplete, // all answered, next question ready
    GameOver       // all answered, no more questions
}

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
