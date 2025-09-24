using System.Text.Json.Serialization;

namespace ChatBackend.Models.GptOss;

public record GptOssResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; } = false;

    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; } = 0;
}
