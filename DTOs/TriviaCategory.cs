using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace trivia_game.DTOs;

public class TriviaCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}