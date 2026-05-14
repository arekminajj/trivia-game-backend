namespace trivia_game.Domain.Entities;

public class TriviaQuestion
{
    public string Text { get; init; } = string.Empty;
    public string CorrectAnswer { get; init; } = string.Empty;
    public List<string> IncorrectAnswers { get; init; } = new();
    public string Category { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
