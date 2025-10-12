using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public record OllamaRequest
{
    [JsonPropertyName("model")]
    required public string Model { get; set; }

    [JsonPropertyName("prompt")]
    required public string Prompt { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; } = null;

    [JsonPropertyName("raw")]
    public bool Raw { get; private set; } = true;

    [JsonPropertyName("stream")]
    public bool Stream { get; private set; } = true;

    [JsonPropertyName("keep_alive")]
    public int? KeepAlive { get; set; } = null;

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; } = null;
}