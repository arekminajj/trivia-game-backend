using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace trivia_game.DTOs;

public class TriviaResponse
{
    [JsonPropertyName("response_code")]
    public int ResponseCode { get; set; }

    [JsonPropertyName("results")]
    public List<TriviaQuestion> Results { get; set; } = new();
}
