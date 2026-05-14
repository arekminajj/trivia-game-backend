using Microsoft.AspNetCore.SignalR;
using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;

namespace trivia_game.Presentation.Hubs;

public class GameHub(IGameService gameService, IGameTimerService timerService) : Hub
{
    /// <summary>
    /// Called after HTTP join/create so the player's SignalR connection joins the room group.
    /// Server → Caller: ConnectedToRoom(RoomResponse)
    /// Server → Others: PlayerConnected(PlayerResponse)
    /// </summary>
    public async Task ConnectToRoom(string roomCode, string playerUuid)
    {
        var result = gameService.ConnectToRoom(roomCode, playerUuid);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.OthersInGroup(roomCode).SendAsync("PlayerConnected", result.Player);
        await Clients.Caller.SendAsync("ConnectedToRoom", result.Room);
    }

    /// <summary>
    /// Owner-only. Transitions room to InProgress and sends the first question to all players.
    /// Server → Group: GameStarted(QuestionResponse)
    /// </summary>
    public async Task StartGame(string roomCode, string playerUuid)
    {
        var result = gameService.StartGame(roomCode, playerUuid);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Clients.Group(roomCode).SendAsync("GameStarted", result.FirstQuestion);
        timerService.StartQuestionTimer(roomCode, result.FirstQuestion!.Index);
    }

    /// <summary>
    /// Records a player's answer. Once all players have answered the round closes automatically.
    /// Server → Caller:  AnswerAccepted
    /// Server → Group:   RoundEnded(RoundResultResponse)
    ///                   then QuestionReceived(QuestionResponse) OR GameEnded(List&lt;PlayerResponse&gt;)
    /// </summary>
    public async Task SubmitAnswer(string roomCode, string playerUuid, string answer)
    {
        var result = gameService.SubmitAnswer(roomCode, playerUuid, answer);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Clients.Caller.SendAsync("AnswerAccepted");

        if (result.Outcome == SubmitAnswerOutcome.Accepted) return;

        // All players answered before the timer — cancel it
        timerService.CancelTimer(roomCode);

        await Clients.Group(roomCode).SendAsync("RoundEnded", result.RoundResult);

        if (result.Outcome == SubmitAnswerOutcome.GameOver)
        {
            await Clients.Group(roomCode).SendAsync("GameEnded", result.FinalLeaderboard);
        }
        else
        {
            await Clients.Group(roomCode).SendAsync("QuestionReceived", result.NextQuestion);
            timerService.StartQuestionTimer(roomCode, result.NextQuestion!.Index);
        }
    }
}
