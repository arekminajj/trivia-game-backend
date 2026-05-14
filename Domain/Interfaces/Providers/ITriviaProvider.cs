using trivia_game.Domain.Entities;

namespace trivia_game.Domain.Interfaces.Providers;

public interface ITriviaProvider
{
    Task<IEnumerable<TriviaQuestion>> GetQuestionsAsync(int? amount, int? categoryId, string? type);
    Task<IEnumerable<TriviaCategory>> GetCategoriesAsync();
}
