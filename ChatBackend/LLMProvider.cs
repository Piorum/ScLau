using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ChatBackend.Models;
using ChatBackend.Models.Ollama;

namespace ChatBackend;


public static class LLMProvider
{
    static public ChannelReader<string> StreamCompletionAsync(string prompt, string modelName, ChatOptions options, Channel<string>? existingChannel = null)
    {
        Channel<string> channel = existingChannel ?? Channel.CreateUnbounded<string>();

        _ = Task.Run(async () =>
        {
            using var client = new HttpClient();
            string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT was null");

            //Other request values will be initialized by environment variables in the constructor
            var requestBody = new OllamaRequest()
            {
                Model = modelName,
                Prompt = prompt,
                Options = new()
                {
                    Temperature = options.Temperature
                }
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

                    if (responseObject is not null && responseObject.Response is not null)
                    {
                        await channel.Writer.WriteAsync(responseObject.Response);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error making request to Ollama: {ex.Message}");
                await channel.Writer.WriteAsync("Error: Could not connect to Ollama.");
            }
            finally
            {
                channel.Writer.Complete();
            }

        });

        return channel.Reader;
    }

}

