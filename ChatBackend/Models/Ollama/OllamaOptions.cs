using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public record OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = double.TryParse(Environment.GetEnvironmentVariable("TEMPERATURE"), out var _temperature) ? _temperature : 1.0;

    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("NUM_PREDICT"), out var _numPredict) ? _numPredict : -2; //Fill context (-1 == Inifinte)

    [JsonPropertyName("num_ctx")]
    public int NumCtx { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("NUM_CTX"), out var _numCtx) ? _numCtx : 8192;

    [JsonPropertyName("repeat_penalty")]
    public double RepeatPenalty { get; set; } = double.TryParse(Environment.GetEnvironmentVariable("REPEAT_PENALTY"), out var _repPenalty) ? _repPenalty : 1.2;

    [JsonPropertyName("top_k")]
    public int TopK { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("TOP_K"), out var _topK) ? _topK : 40;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = double.TryParse(Environment.GetEnvironmentVariable("TOP_P"), out var _topP) ? _topP : 0.95;

    [JsonPropertyName("min_p")]
    public double MinP { get; set; } = double.TryParse(Environment.GetEnvironmentVariable("MIN_P"), out var _minP) ? _minP : 0.05;

}
