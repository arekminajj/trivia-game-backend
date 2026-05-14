namespace trivia_game.Application.Interfaces;

public interface IGameTimerService
{
    void StartQuestionTimer(string roomCode, int questionIndex);
    void CancelTimer(string roomCode);
}
