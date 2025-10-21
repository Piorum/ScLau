using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;
using ChatBackend.Models.Ollama;

namespace ChatBackend;


public class OllamaProvider(HttpClient client) : ILLMProvider
{
    private readonly HttpClient _client = client;
    private readonly JsonSerializerOptions defaultRequestOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, string modelName, ChatOptions options, CancellationToken cancellationToken = default)
    {
        Channel<string> channel = Channel.CreateUnbounded<string>();

        _ = Task.Run(async () =>
        {
            var requestBody = new OllamaRequest()
            {
                Model = modelName,
                Prompt = prompt,
                Options = options.ModelOptions
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, defaultRequestOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            try
            {
                using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var responseObject = JsonSerializer.Deserialize<OllamaResponse>(line);

                    if (responseObject is not null && responseObject.Response is not null)
                    {
                        await channel.Writer.WriteAsync(responseObject.Response, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Expected just continue to finally block
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

        }, CancellationToken.None);

        return channel.Reader.ReadAllAsync(CancellationToken.None);
    }

}

