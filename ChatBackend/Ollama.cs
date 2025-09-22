using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using ChatBackend.Builders;

namespace ChatBackend;

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

public class OllamaOptions
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

public class OllamaResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; set; }
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

public static class Ollama
{
    private static readonly GptOssChatBuilder history;

    static Ollama()
    {
        var gopb = new GptOssPromptBuilder()
            .WithSystemMessage("You are a large language model (LLM).")
            .WithDeveloperInstructions("Fulfill the request to the best of your abilities")
            .WithReasoningLevel(Models.GptOssReasoningLevel.Medium);
        history = new GptOssChatBuilder().WithPrompt(gopb);
    }

    public static ChannelReader<OllamaResponse> GetCompletion(string prompt)
    {
        var channel = Channel.CreateUnbounded<OllamaResponse>();
        _ = Task.Run(async () =>
        {
            var client = new HttpClient();
            string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT was null");

            history.Append(new(Models.GptOssRole.User, Models.GptOssChannel.None, prompt));

            var requestBody = new OllamaRequest
            {
                Prompt = history.WithAssistantTrail(),
                Raw = true
            };
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            };

            StringBuilder responseBuilder = new();
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var responseObject = JsonSerializer.Deserialize<OllamaResponse>(line);

                    if (responseObject is not null)
                    {
                        responseBuilder.Append(responseObject.Response);
                        await channel.Writer.WriteAsync(responseObject);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Log the error or handle it as needed
                Console.WriteLine($"Error making request to Ollama: {ex.Message}");
                // Optionally write an error message to the channel
                await channel.Writer.WriteAsync(new OllamaResponse { Response = "Error: Could not connect to Ollama.", Done = true });
            }
            finally
            {
                channel.Writer.Complete();
            }
            history.AppendRaw($"<|start|>assistant{responseBuilder}");
        });

        return channel.Reader;
    }
}

