using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Options;
using trivia_game.Presentation.Hubs;

namespace trivia_game.Presentation.Services;

public class GameTimerService(
    IGameService gameService,
    IHubContext<GameHub> hubContext,
    IOptions<GameOptions> options) : IGameTimerService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();
    private readonly int _timeoutSeconds = options.Value.QuestionTimeoutSeconds;

    public void StartQuestionTimer(string roomCode, int questionIndex)
    {
        CancelTimer(roomCode);
        var cts = new CancellationTokenSource();
        _timers[roomCode] = cts;
        _ = RunAsync(roomCode, questionIndex, cts.Token);
    }

    public void CancelTimer(string roomCode)
    {
        if (_timers.TryRemove(roomCode, out var cts))
            cts.Cancel();
    }

    private async Task RunAsync(string roomCode, int questionIndex, CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), ct); }
        catch (OperationCanceledException) { return; }

        var result = gameService.TimeOutRound(roomCode, questionIndex);
        if (!result.Success) return;

        await hubContext.Clients.Group(roomCode).SendAsync("QuestionTimedOut");
        await hubContext.Clients.Group(roomCode).SendAsync("RoundEnded", result.RoundResult);

        if (result.Outcome == SubmitAnswerOutcome.GameOver)
        {
            await hubContext.Clients.Group(roomCode).SendAsync("GameEnded", result.FinalLeaderboard);
        }
        else
        {
            await hubContext.Clients.Group(roomCode).SendAsync("QuestionReceived", result.NextQuestion);
            StartQuestionTimer(roomCode, result.NextQuestion!.Index);
        }
    }
}
