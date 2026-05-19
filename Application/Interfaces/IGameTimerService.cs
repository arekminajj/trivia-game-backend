using trivia_game.Application.DTOs;

namespace trivia_game.Application.Interfaces;

public interface IGameTimerService
{
    void StartQuestionTimer(string roomCode, int questionIndex);
    void CancelTimer(string roomCode);
    void StartReadyTimer(string roomCode, QuestionResponse? nextQuestion, List<PlayerResponse>? finalLeaderboard);
    bool CancelReadyTimer(string roomCode);
}
