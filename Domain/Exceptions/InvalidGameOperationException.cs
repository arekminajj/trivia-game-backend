namespace trivia_game.Domain.Exceptions;

public class InvalidGameOperationException(string message) : Exception(message);
