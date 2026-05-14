namespace trivia_game.Application.DTOs;

public sealed record RoundResultResponse(
    string CorrectAnswer,
    List<PlayerResponse> Scores);
