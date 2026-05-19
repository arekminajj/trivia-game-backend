namespace trivia_game.Application.Options;

public class GameOptions
{
    public int QuestionTimeoutSeconds { get; set; } = 30;
    public int ReadyTimeoutSeconds { get; set; } = 10;
}
