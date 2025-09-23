using System.Text.Json.Serialization;
using ChatBackend.Models.GptOss;

namespace ChatBackend.Models.Ollama;

public class OllamaResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; set; }
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = GptOssChannel.None.ToString();
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}
