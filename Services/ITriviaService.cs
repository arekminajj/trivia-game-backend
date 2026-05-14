
using trivia_game.DTOs;

namespace trivia_game.Services;

public interface ITriviaService
{
    public Task<List<TriviaQuestion?>> GetTriviaQuestionsAsync(int? amount, int? category, string type);
    public Task<List<TriviaCategory?>> GetTriviaCategories();
}