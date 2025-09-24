using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public record OllamaRequest
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("model")]
    required public string Model { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions Options { get; set; } = new();

    //This is here to get seralized into the json request, Ollama.cs expects this to be true
    [JsonPropertyName("raw")]
    public bool Raw { get; private set; } = true;
}