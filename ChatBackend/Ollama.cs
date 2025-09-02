using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace ChatBackend;

public class OllamaRequest
{
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
    [JsonPropertyName("raw")]
    public bool Raw { get; set; } = true;
    [JsonPropertyName("stop")]
    public string[]? StopTokens { get; set; } = ["<|return|>", "<|call|>"];
}

public class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1.0;
    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; } = -2; //Fill context (-1 == Inifinte)
    [JsonPropertyName("num_ctx")]
    public int NumCtx { get; set; } = 8192;
    [JsonPropertyName("repeat_penalty")]
    public double RepeatPenalty { get; set; } = 1.0;
    [JsonPropertyName("top_k")]
    public int TopK { get; set; } = 40;
    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.95;
    [JsonPropertyName("min_p")]
    public double MinP { get; set; } = 0.05;

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
    public static ChannelReader<OllamaResponse> GetCompletion(string prompt)
    {
        var channel = Channel.CreateUnbounded<OllamaResponse>();
        _ = Task.Run(async () =>
        {
            var client = new HttpClient();
            string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT was null");
            var requestBody = new OllamaRequest
            {
                Prompt = "<|start|>system<|message|>You are a large language model (LLM).\nKnowledge cutoff: 2024-06\nCurrent date: 2025-06-28\n\nReasoning: high\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message.\nCalls to these tools must go to the commentary channel: 'functions'.<|end|><|start|>developer<|message|># Instructions\n\nFulfill the request to the best of your abilities\n\n# Tools\n\n## functions\n\nnamespace functions {\n\n// Gets the location of the user.\ntype get_location = () => any;\n\n// Gets the current weather in the provided location.\ntype get_current_weather = (_: {\n// The city and state, e.g. San Francisco, CA\nlocation: string,\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n// Gets the current weather in the provided list of locations.\ntype get_multiple_weathers = (_: {\n// List of city and state, e.g. [\"San Francisco, CA\", \"New York, NY\"]\nlocations: string[],\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n} // namespace functions<|end|><|start|>user<|message|>" + prompt + "<|end|><|start|>assistant",
                //Base Model: (https://huggingface.co/huihui-ai/Huihui-gpt-oss-20b-BF16-abliterated) | Stop tokens adjusted via Modelfile (["<|return|>", "<|call|>"])
                Model = "gpt-oss-20b-abliterated:latest",
                Options = new OllamaOptions
                {
                    Temperature = 0.6,
                    NumPredict = 2000,
                    NumCtx = 8192,
                    RepeatPenalty = 1.15
                },
                Raw = true,
                StopTokens = null
            };
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            };

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
        });

        return channel.Reader;
    }
}

