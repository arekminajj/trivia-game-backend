using trivia_game.Application.DTOs;

namespace trivia_game.Application.Interfaces;

public interface IGameService
{
    ConnectToRoomResult ConnectToRoom(string roomCode, string playerUuid);
    StartGameResult StartGame(string roomCode, string playerUuid);
    SubmitAnswerResult SubmitAnswer(string roomCode, string playerUuid, string answer);
}
