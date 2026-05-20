using trivia_game.Application.DTOs;
using trivia_game.Domain.Entities;

namespace trivia_game.Application.Interfaces;

public interface IGameService
{
    ConnectToRoomResult ConnectToRoom(string roomCode, string playerUuid);
    StartGameResult StartGame(string roomCode, string playerUuid);
    SubmitAnswerResult SubmitAnswer(string roomCode, string playerUuid, string answer);
    SubmitAnswerResult TimeOutRound(string roomCode, int questionIndex);
    DisconnectFromRoomResult DisconnectFromRoom(string roomCode, string playerUuid);
    SignalReadyResult SignalPlayerReady(string roomCode, string playerUuid);
    RestartGameResult RestartGame(string roomCode, string playerUuid, List<TriviaQuestion> questions, string categoryName);
}
