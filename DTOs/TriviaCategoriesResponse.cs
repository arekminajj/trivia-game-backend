using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace trivia_game.DTOs;

public class TriviaCategoriesResponse
{
    [JsonPropertyName("trivia_categories")]
    public List<TriviaCategory> TriviaCategories { get; set; } = new();
}
