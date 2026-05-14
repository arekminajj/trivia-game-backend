using trivia_game.DTOs;

namespace trivia_game.Clients;

public interface ITriviaClient
{
    public Task<IEnumerable<TriviaQuestion?>> GetTriviaQuestionsAsync(int? amount, int? category, string? type);
    public Task<IEnumerable<TriviaCategory?>> GetTriviaCategoriesAsync();
}