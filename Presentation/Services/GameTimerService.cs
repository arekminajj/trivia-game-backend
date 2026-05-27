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
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _readyTimers = new();
    private readonly int _timeoutSeconds = options.Value.QuestionTimeoutSeconds;
    private readonly int _readyTimeoutSeconds = options.Value.ReadyTimeoutSeconds;

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

    public void StartReadyTimer(string roomCode, QuestionResponse? nextQuestion, List<PlayerResponse>? finalLeaderboard)
    {
        CancelReadyTimer(roomCode);
        var cts = new CancellationTokenSource();
        _readyTimers[roomCode] = cts;
        _ = RunReadyAsync(roomCode, nextQuestion, finalLeaderboard, cts.Token);
    }

    public bool CancelReadyTimer(string roomCode)
    {
        if (_readyTimers.TryRemove(roomCode, out var cts))
        {
            cts.Cancel();
            return true;
        }
        return false;
    }

    private async Task RunReadyAsync(string roomCode, QuestionResponse? nextQuestion, List<PlayerResponse>? finalLeaderboard, CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(_readyTimeoutSeconds), ct); }
        catch (OperationCanceledException) { return; }

        _readyTimers.TryRemove(roomCode, out _);

        if (finalLeaderboard != null)
            await hubContext.Clients.Group(roomCode).SendAsync("GameEnded", finalLeaderboard);
        else if (nextQuestion != null)
        {
            await hubContext.Clients.Group(roomCode).SendAsync("QuestionReceived", nextQuestion);
            StartQuestionTimer(roomCode, nextQuestion.Index);
        }
    }

    private async Task RunAsync(string roomCode, int questionIndex, CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), ct); }
        catch (OperationCanceledException) { return; }

        var result = gameService.TimeOutRound(roomCode, questionIndex);
        if (!result.Success) return;

        await hubContext.Clients.Group(roomCode).SendAsync("QuestionTimedOut");
        await hubContext.Clients.Group(roomCode).SendAsync("RoundEnded", result.RoundResult);
        StartReadyTimer(roomCode, result.NextQuestion, result.FinalLeaderboard);
    }
}
