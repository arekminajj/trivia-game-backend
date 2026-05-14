using trivia_game.DTOs;
using Microsoft.AspNetCore.WebUtilities;

namespace trivia_game.Clients;

public class TriviaClient : ITriviaClient
{
    private readonly HttpClient _httpClient;
    public TriviaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri("https://opentdb.com/");
    }

    public async Task<IEnumerable<TriviaQuestion?>> GetTriviaQuestionsAsync(
        int? amount,
        int? category,
        string? type)
    {


        if (amount.HasValue)
        {
            amount = Math.Clamp(amount.Value, 1, 50);
        }

        var normalizedType = type?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedType) &&
            normalizedType != "multiple" &&
            normalizedType != "boolean")
        {
            throw new ArgumentException("Type must be either 'multiple' or 'boolean'");
        }

        var query = new Dictionary<string, string?>();

        if (amount.HasValue)
            query["amount"] = amount.Value.ToString();

        if (category.HasValue)
            query["category"] = category.Value.ToString();

        if (!string.IsNullOrWhiteSpace(normalizedType))
            query["type"] = normalizedType;

        var url = QueryHelpers.AddQueryString("/api.php", query);

        var response = await _httpClient
            .GetFromJsonAsync<TriviaResponse>(url);

        return response?.Results ?? Enumerable.Empty<TriviaQuestion>();
    }

    public async Task<IEnumerable<TriviaCategory?>> GetTriviaCategoriesAsync()
    {
        var url = "/api_category.php";
        var response = await _httpClient.GetFromJsonAsync<TriviaCategoriesResponse>(url);

        return response?.TriviaCategories ?? Enumerable.Empty<TriviaCategory>();
    }
}