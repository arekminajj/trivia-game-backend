using trivia_game.Application.Interfaces;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Interfaces.Providers;

namespace trivia_game.Application.Services;

public class TriviaService(ITriviaProvider triviaProvider) : ITriviaService
{
    public async Task<List<TriviaQuestion>> GetQuestionsAsync(int? amount, int? categoryId, string? type)
    {
        var questions = await triviaProvider.GetQuestionsAsync(amount, categoryId, type);
        return questions.ToList();
    }

    public async Task<List<TriviaCategory>> GetCategoriesAsync()
    {
        var categories = await triviaProvider.GetCategoriesAsync();
        return categories.ToList();
    }
}
