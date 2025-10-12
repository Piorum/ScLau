using System.Text.Json.Serialization;

namespace ChatBackend.Models.Ollama;

public record OllamaOptions
{
    [JsonPropertyName("num_keep")]
    public int? NumKeep { get; set; } = null;

    [JsonPropertyName("seed")]
    public int? Seed { get; set; } = null;

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; } = null;

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; } = null;

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; } = null;

    [JsonPropertyName("min_p")]
    public float? MinP { get; set; } = null;

    [JsonPropertyName("typical_p")]
    public float? TypicalP { get; set; } = null;

    [JsonPropertyName("repeat_last_n")]
    public int? RepeatLastN { get; set; } = null;

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; } = null;

    [JsonPropertyName("repeat_penalty")]
    public float? RepeatPenalty { get; set; } = null;

    [JsonPropertyName("presenecy_penalty")]
    public float? PresencePenalty { get; set; } = null;

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; } = null;

    [JsonPropertyName("mirostat")]
    public int? Mirostat { get; set; } = null;

    [JsonPropertyName("mirostat_tau")]
    public float? MirostatTau { get; set; } = null;

    [JsonPropertyName("mirostat_eta")]
    public float? MirostatEta { get; set; } = null;

    [JsonPropertyName("penalize_newline")]
    public bool? PenalizeNewline { get; set; } = null;

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; } = null;

    [JsonPropertyName("numa")]
    public bool? Numa { get; set; } = null;

    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; } = null;

    [JsonPropertyName("num_batch")]
    public int? NumBatch { get; set; } = null;

    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; } = null;

    [JsonPropertyName("main_gpu")]
    public int? MainGpu { get; set; } = null;

    [JsonPropertyName("low_vram")]
    public bool? LowVram { get; set; } = null;

    [JsonPropertyName("vocab_only")]
    public bool? VocabOnly { get; set; } = null;

    [JsonPropertyName("use_nmap")]
    public bool? UseNmap { get; set; } = null;

    [JsonPropertyName("use_mlock")]
    public bool? UseMlock { get; set; } = null;

    [JsonPropertyName("num_thread")]
    public int? NumThread { get; set; } = null;

}
