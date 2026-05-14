using System.Text.Json.Serialization;

namespace trivia_game.Infrastructure.ExternalApis.OpenTdb.Models;

internal class OpenTdbResponse
{
    [JsonPropertyName("response_code")] public int ResponseCode { get; set; }
    [JsonPropertyName("results")] public List<OpenTdbQuestion> Results { get; set; } = new();
}

internal class OpenTdbQuestion
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("question")] public string Question { get; set; } = string.Empty;
    [JsonPropertyName("correct_answer")] public string CorrectAnswer { get; set; } = string.Empty;
    [JsonPropertyName("incorrect_answers")] public List<string> IncorrectAnswers { get; set; } = new();
}

internal class OpenTdbCategoriesResponse
{
    [JsonPropertyName("trivia_categories")] public List<OpenTdbCategory> TriviaCategories { get; set; } = new();
}

internal class OpenTdbCategory
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}
