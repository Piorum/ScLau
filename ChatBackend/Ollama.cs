using System.Text;
using System.Text.Json;

namespace ChatBackend;

public static class Ollama
{
    public static async IAsyncEnumerable<string> GetCompletion(string userPrompt)
    {
        using var client = new HttpClient();
        string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT is null.");

        var requestBody = new
        {
            prompt = "<|start|>system<|message|>You are ChatGPT, a large language model trained by OpenAI.\nKnowledge cutoff: 2024-06\nCurrent date: 2025-06-28\n\nReasoning: high\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message.\nCalls to these tools must go to the commentary channel: 'functions'.<|end|><|start|>developer<|message|># Instructions\n\nUse a friendly tone.\n\n# Tools\n\n## functions\n\nnamespace functions {\n\n// Gets the location of the user.\ntype get_location = () => any;\n\n// Gets the current weather in the provided location.\ntype get_current_weather = (_: {\n// The city and state, e.g. San Francisco, CA\nlocation: string,\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n// Gets the current weather in the provided list of locations.\ntype get_multiple_weathers = (_: {\n// List of city and state, e.g. [\"San Francisco, CA\", \"New York, NY\"]\nlocations: string[],\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n} // namespace functions<|end|><|start|>user<|message|>" + userPrompt + "<|end|><|start|>assistant",
            model = "gpt-oss:20b",
            options = new
            {
                temperature = 0.95,
                num_predict = 2000,
                num_ctx = 4096,
            },
            raw = true
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await foreach (var chunk in ReadStream(response.Content))
        {
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<string> ReadStream(HttpContent content)
    {
        await Console.Out.WriteAsync("Getting Model Response");

        int tokenCount = 0;
        using (var stream = await content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                var chunk = JsonDocument.Parse(line);
                string? textChunk = chunk.RootElement.GetProperty("response").GetString();
                bool done = chunk.RootElement.GetProperty("done").GetBoolean();

                if (!string.IsNullOrEmpty(textChunk))
                {
                    yield return textChunk;
                    tokenCount++;
                }

                if (done) break;
            }
        }

        await Console.Out.WriteLineAsync($"Generated {tokenCount} Tokens");
    }
}