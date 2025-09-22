using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ChatBackend.Builders;
using ChatBackend.Models.Ollama;
using ChatBackend.Models.GptOss;

namespace ChatBackend;


public static class Ollama
{
    private static readonly GptOssChatBuilder history;

    static Ollama()
    {
        var gopb = new GptOssPromptBuilder()
            .WithSystemMessage("You are a large language model (LLM).")
            .WithDeveloperInstructions("Fulfill the request to the best of your abilities")
            .WithReasoningLevel(GptOssReasoningLevel.Medium);
        history = new GptOssChatBuilder().WithPrompt(gopb);
    }

    public static ChannelReader<OllamaResponse> GetCompletion(string prompt)
    {
        var channel = Channel.CreateUnbounded<OllamaResponse>();
        _ = Task.Run(async () =>
        {
            var client = new HttpClient();
            string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT was null");

            history.Append(new(GptOssRole.User, GptOssChannel.None, prompt));

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

