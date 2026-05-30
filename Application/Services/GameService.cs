using System.Collections.Concurrent;
using trivia_game.Application.DTOs;
using trivia_game.Application.Interfaces;
using trivia_game.Application.Mappings;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Enums;
using trivia_game.Domain.Interfaces.Repositories;

namespace trivia_game.Application.Services;

public class GameService(IRoomRepository roomRepository) : IGameService
{
    private static readonly ConcurrentDictionary<string, object> _roomLocks = new();
    private static object RoomLock(string roomCode) => _roomLocks.GetOrAdd(roomCode, _ => new object());
    public ConnectToRoomResult ConnectToRoom(string roomCode, string playerUuid)
    {
        if (!roomRepository.TryGet(roomCode, out var room))
            return new ConnectToRoomResult(false, $"Room '{roomCode}' not found.");

        var player = room.Members.FirstOrDefault(m => m.Uuid == playerUuid);
        if (player is null)
            return new ConnectToRoomResult(false, "Player not found in room.");

        return new ConnectToRoomResult(true,
            Player: RoomMapper.ToPlayer(player),
            Room: RoomMapper.ToRoom(room));
    }

    public StartGameResult StartGame(string roomCode, string playerUuid)
    {
        if (!roomRepository.TryGet(roomCode, out var room))
            return new StartGameResult(false, $"Room '{roomCode}' not found.");

        if (room.Owner.Uuid != playerUuid)
            return new StartGameResult(false, "Only the room owner can start the game.");

        if (!room.Start())
            return new StartGameResult(false, "Game cannot be started (already started or no questions).");

        roomRepository.Save(room);
        return new StartGameResult(true, FirstQuestion: RoomMapper.ToQuestion(room));
    }

    public SubmitAnswerResult SubmitAnswer(string roomCode, string playerUuid, string answer)
    {
        lock (RoomLock(roomCode))
        {
            if (!roomRepository.TryGet(roomCode, out var room))
                return new SubmitAnswerResult(false, $"Room '{roomCode}' not found.");

            if (!room.SubmitAnswer(playerUuid, answer))
                return new SubmitAnswerResult(false, "Could not record answer (game not in progress, unknown player, or invalid answer).");

            if (!room.AllPlayersAnswered())
            {
                roomRepository.Save(room);
                return new SubmitAnswerResult(true, Outcome: SubmitAnswerOutcome.Accepted);
            }

            return FinalizeRound(room);
        }
    }

    public SubmitAnswerResult TimeOutRound(string roomCode, int questionIndex)
    {
        lock (RoomLock(roomCode))
        {
            if (!roomRepository.TryGet(roomCode, out var room))
                return new SubmitAnswerResult(false, $"Room '{roomCode}' not found.");

            if (room.Status != RoomStatus.InProgress || room.CurrentQuestionIndex != questionIndex)
                return new SubmitAnswerResult(false, "Round already advanced.");

            foreach (var member in room.Members.Where(m => !room.CurrentRoundAnswers.ContainsKey(m.Uuid)))
                room.SubmitAnswer(member.Uuid, string.Empty);

            return FinalizeRound(room);
        }
    }

    public DisconnectFromRoomResult DisconnectFromRoom(string roomCode, string playerUuid)
    {
        if (!roomRepository.TryGet(roomCode, out var room))
            return new DisconnectFromRoomResult(false, $"Room '{roomCode}' not found.");

        bool wasInProgress = room.Status == RoomStatus.InProgress;
        bool hadAnswered = room.CurrentRoundAnswers.ContainsKey(playerUuid);
        bool isOwner = room.Owner.Uuid == playerUuid;

        if (!room.RemoveMember(playerUuid))
            return new DisconnectFromRoomResult(false, "Player not found in room.");

        if (room.Members.Count == 0)
        {
            roomRepository.Remove(roomCode);
            return new DisconnectFromRoomResult(true, Outcome: DisconnectOutcome.RoomEmpty);
        }

        if (isOwner && !wasInProgress)
        {
            roomRepository.Remove(roomCode);
            return new DisconnectFromRoomResult(true, Outcome: DisconnectOutcome.RoomClosedByHost, RoomCode: roomCode);
        }

        if (wasInProgress && !hadAnswered && room.AllPlayersAnswered())
        {
            var finalized = FinalizeRound(room);
            return new DisconnectFromRoomResult(true,
                Outcome: finalized.Outcome == SubmitAnswerOutcome.GameOver
                    ? DisconnectOutcome.GameOver
                    : DisconnectOutcome.RoundComplete,
                RoomCode: roomCode,
                RoundResult: finalized.RoundResult,
                NextQuestion: finalized.NextQuestion,
                FinalLeaderboard: finalized.FinalLeaderboard);
        }

        if (wasInProgress && room.AllPlayersReady())
        {
            room.ClearReady();
            roomRepository.Save(room);
            if (room.Status == RoomStatus.Finished)
            {
                var leaderboard = room.Members
                    .OrderByDescending(m => m.Points)
                    .Select(RoomMapper.ToPlayer)
                    .ToList();
                return new DisconnectFromRoomResult(true,
                    Outcome: DisconnectOutcome.ReadyPhaseComplete,
                    RoomCode: roomCode,
                    FinalLeaderboard: leaderboard);
            }
            return new DisconnectFromRoomResult(true,
                Outcome: DisconnectOutcome.ReadyPhaseComplete,
                RoomCode: roomCode,
                NextQuestion: RoomMapper.ToQuestion(room));
        }

        roomRepository.Save(room);
        return new DisconnectFromRoomResult(true, Outcome: DisconnectOutcome.PlayerRemoved, RoomCode: roomCode);
    }

    public SignalReadyResult SignalPlayerReady(string roomCode, string playerUuid)
    {
        lock (RoomLock(roomCode))
        {
            if (!roomRepository.TryGet(roomCode, out var room))
                return new SignalReadyResult(false, Error: $"Room '{roomCode}' not found.");

            room.SignalReady(playerUuid);

            if (!room.AllPlayersReady())
            {
                roomRepository.Save(room);
                return new SignalReadyResult(true, AllReady: false);
            }

            room.ClearReady();
            roomRepository.Save(room);

            if (room.Status == RoomStatus.Finished)
            {
                var leaderboard = room.Members
                    .OrderByDescending(m => m.Points)
                    .Select(RoomMapper.ToPlayer)
                    .ToList();
                return new SignalReadyResult(true, AllReady: true, FinalLeaderboard: leaderboard);
            }

            return new SignalReadyResult(true, AllReady: true, NextQuestion: RoomMapper.ToQuestion(room));
        }
    }

    public RestartGameResult RestartGame(string roomCode, string playerUuid, List<TriviaQuestion> questions, string categoryName)
    {
        if (!roomRepository.TryGet(roomCode, out var room))
            return new RestartGameResult(false, $"Room '{roomCode}' not found.");

        if (room.Owner.Uuid != playerUuid)
            return new RestartGameResult(false, "Only the room owner can restart the game.");

        room.Restart(questions, categoryName);
        roomRepository.Save(room);

        return new RestartGameResult(true, Room: RoomMapper.ToRoom(room));
    }

    private SubmitAnswerResult FinalizeRound(Room room)
    {
        var correctAnswer = room.CurrentQuestion!.CorrectAnswer;
        room.ScoreCurrentRound();

        var roundResult = new RoundResultResponse(
            correctAnswer,
            room.Members.Select(RoomMapper.ToPlayer).ToList());

        var hasNext = room.AdvanceQuestion();
        roomRepository.Save(room);

        if (!hasNext)
        {
            var leaderboard = room.Members
                .OrderByDescending(m => m.Points)
                .Select(RoomMapper.ToPlayer)
                .ToList();
            return new SubmitAnswerResult(true,
                Outcome: SubmitAnswerOutcome.GameOver,
                RoundResult: roundResult,
                FinalLeaderboard: leaderboard);
        }

        return new SubmitAnswerResult(true,
            Outcome: SubmitAnswerOutcome.RoundComplete,
            RoundResult: roundResult,
            NextQuestion: RoomMapper.ToQuestion(room));
    }
}
