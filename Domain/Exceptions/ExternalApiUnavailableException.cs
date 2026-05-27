namespace trivia_game.Domain.Exceptions;

public class ExternalApiUnavailableException(string message) : Exception(message);
