namespace trivia_game.Domain.Exceptions;

public class RoomNotFoundException(string joinCode)
    : Exception($"Room with join code '{joinCode}' was not found.");
