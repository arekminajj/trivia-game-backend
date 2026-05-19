using Microsoft.AspNetCore.WebUtilities;
using trivia_game.Domain.Entities;
using trivia_game.Domain.Exceptions;
using trivia_game.Domain.Interfaces.Providers;
using trivia_game.Infrastructure.ExternalApis.OpenTdb.Models;

namespace trivia_game.Infrastructure.ExternalApis.OpenTdb;

public class OpenTdbClient(HttpClient httpClient) : ITriviaProvider
{
    public async Task<IEnumerable<TriviaQuestion>> GetQuestionsAsync(int? amount, int? categoryId, string? type)
    {
        var normalizedType = type?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedType) &&
            normalizedType != "multiple" && normalizedType != "boolean")
            throw new ArgumentException("Type must be 'multiple' or 'boolean'.");

        if (amount.HasValue)
            amount = Math.Clamp(amount.Value, 1, 50);

        var query = new Dictionary<string, string?>();
        if (amount.HasValue) query["amount"] = amount.Value.ToString();
        if (categoryId.HasValue) query["category"] = categoryId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(normalizedType)) query["type"] = normalizedType;

        var url = QueryHelpers.AddQueryString("/api.php", query);

        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenTdbResponse>(url);

            if (response is null)
                throw new ExternalApiUnavailableException("Trivia API returned an empty response.");

            // response_code 1 = not enough questions for the given filter combination
            if (response.ResponseCode == 1)
                throw new ArgumentException("Not enough questions available for the selected filters. Try reducing the amount or changing the category.");

            if (response.ResponseCode != 0)
                throw new ExternalApiUnavailableException($"Trivia API returned error code {response.ResponseCode}.");

            return response.Results.Select(Map);
        }
        catch (ExternalApiUnavailableException) { throw; }
        catch (ArgumentException) { throw; }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiUnavailableException($"Trivia API is currently unavailable: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new ExternalApiUnavailableException("Request to trivia API timed out. Please try again later.");
        }
    }

    public async Task<IEnumerable<TriviaCategory>> GetCategoriesAsync()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenTdbCategoriesResponse>("/api_category.php");
            return response?.TriviaCategories.Select(c => new TriviaCategory { Id = c.Id, Name = c.Name })
                   ?? Enumerable.Empty<TriviaCategory>();
        }
        catch (ExternalApiUnavailableException) { throw; }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiUnavailableException($"Trivia API is currently unavailable: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new ExternalApiUnavailableException("Request to trivia API timed out. Please try again later.");
        }
    }

    private static TriviaQuestion Map(OpenTdbQuestion q) => new()
    {
        Text = q.Question,
        CorrectAnswer = q.CorrectAnswer,
        IncorrectAnswers = q.IncorrectAnswers,
        Category = q.Category,
        Difficulty = q.Difficulty,
        Type = q.Type
    };
}
