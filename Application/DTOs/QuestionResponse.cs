namespace trivia_game.Application.DTOs;

public sealed record QuestionResponse(
    int Index,
    int Total,
    string Text,
    string Category,
    string Difficulty,
    string Type,
    List<string> Answers);
