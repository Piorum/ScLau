using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public record OllamaResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; set; }
}
