using trivia_game.Models;
using trivia_game.Clients;
using trivia_game.DTOs;

namespace trivia_game.Services;

public class TriviaService : ITriviaService
{
    private readonly ITriviaClient _triviaClient;

    public TriviaService(ITriviaClient triviaClient)
    {
        _triviaClient = triviaClient;
    }

    public async Task<List<TriviaQuestion?>> GetTriviaQuestionsAsync(int? amount, int? category, string type)
    {
        try
        {
            var questions = await _triviaClient.GetTriviaQuestionsAsync(amount, category, type);
            return questions.ToList();
        }
        catch (HttpRequestException)
        {
            //TODO: LOG ERROR
            return new List<TriviaQuestion?>();
        }
    }

    public async Task<List<TriviaCategory?>> GetTriviaCategories()
    {
        try
        {
            var categories = await _triviaClient.GetTriviaCategoriesAsync();
            return categories.ToList();
        }
        catch (HttpRequestException)
        {
            //TODO: LOG ERROR
            return new List<TriviaCategory?>();
        }
    }

}