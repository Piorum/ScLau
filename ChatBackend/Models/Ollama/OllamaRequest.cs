using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public class OllamaRequest
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-oss:20b";
    [JsonPropertyName("options")]
    public OllamaOptions Options { get; set; } = new();
    [JsonPropertyName("raw")]
    public bool Raw { get; set; } = true;
}