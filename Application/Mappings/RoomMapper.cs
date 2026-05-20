using trivia_game.Application.DTOs;
using trivia_game.Domain.Entities;

namespace trivia_game.Application.Mappings;

internal static class RoomMapper
{
    public static PlayerResponse ToPlayer(RoomMember m) =>
        new(m.Uuid, m.DisplayName, m.Points, m.CorrectAnswers);

    public static RoomResponse ToRoom(Room room) => new(
        room.Uuid,
        room.JoinCode,
        room.Status,
        ToPlayer(room.Owner),
        room.Members.Select(ToPlayer).ToList(),
        room.Questions.Count,
        room.CurrentQuestionIndex,
        room.CategoryName);

    public static QuestionResponse ToQuestion(Room room) => new(
        room.CurrentQuestionIndex,
        room.Questions.Count,
        room.CurrentQuestion!.Text,
        room.CurrentQuestion.Category,
        room.CurrentQuestion.Difficulty,
        room.CurrentQuestion.Type,
        room.CurrentShuffledAnswers);
}
