using trivia_game.Domain.Enums;

namespace trivia_game.Application.DTOs;

public sealed record RoomResponse(
    string Uuid,
    string JoinCode,
    RoomStatus Status,
    PlayerResponse Owner,
    List<PlayerResponse> Members,
    int TotalQuestions,
    int CurrentQuestionIndex,
    string CategoryName);

public sealed record PlayerResponse(
    string Uuid,
    string DisplayName,
    int Points,
    int CorrectAnswers);
