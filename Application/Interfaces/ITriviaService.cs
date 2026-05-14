using trivia_game.Domain.Entities;

namespace trivia_game.Application.Interfaces;

public interface ITriviaService
{
    Task<List<TriviaQuestion>> GetQuestionsAsync(int? amount, int? categoryId, string? type);
    Task<List<TriviaCategory>> GetCategoriesAsync();
}
