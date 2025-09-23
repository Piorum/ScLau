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

    public static ChannelReader<OllamaResponse> GetCompletion(string? prompt, Channel<OllamaResponse>? existingReader = null)
    {
        Channel<OllamaResponse> channel = existingReader ?? Channel.CreateUnbounded<OllamaResponse>();
        _ = Task.Run(async () =>
        {
            var client = new HttpClient();
            string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new("OLLAMA_ENDPOINT was null");

            if(prompt is not null)
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
            bool incomingRole = false;
            bool incomingChannel = false;
            bool incomingMessage = false;
            GptOssChannel currentChannel = GptOssChannel.None;
            GptOssRole currentRole = GptOssRole.Assistant;

            try
            {

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    await Console.Out.WriteLineAsync(line);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var responseObject = JsonSerializer.Deserialize<OllamaResponse>(line);

                    if (responseObject is not null)
                    {
                        switch (responseObject.Response)
                        {
                            case "<|start|>":
                                incomingRole = true;
                                continue;
                            case "<|channel|>":
                                incomingChannel = true;
                                continue;
                            case "<|message|>":
                                incomingMessage = true;
                                continue;
                            case "<|end|>":
                                incomingMessage = false;
                                history.Append(new(currentRole, currentChannel, responseBuilder.ToString()));
                                responseBuilder.Clear();
                                continue;
                        }

                        if (incomingMessage)
                        {
                            responseBuilder.Append(responseObject.Response);

                            responseObject.Channel = currentChannel.ToString();

                            if (currentChannel != GptOssChannel.Final)
                                responseObject.Done = false;
                            await channel.Writer.WriteAsync(responseObject);
                        }
                        else if (incomingChannel)
                        {
                            currentChannel = responseObject.Response switch
                            {
                                "analysis" => GptOssChannel.Analysis,
                                "commentary" => GptOssChannel.Commentary,
                                "final" => GptOssChannel.Final,
                                _ => GptOssChannel.None
                            };
                            incomingChannel = false;
                        }
                        else if (incomingRole)
                        {
                            currentRole = responseObject.Response switch
                            {
                                "assistant" => GptOssRole.Assistant,
                                "developer" => GptOssRole.Developer,
                                "system" => GptOssRole.System,
                                "user" => GptOssRole.User,
                                _ => GptOssRole.None
                            };
                            incomingRole = false;
                        }
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

                if (currentChannel == GptOssChannel.Final)
                {
                    responseBuilder.Append("<|return|>");
                    history.Append(new(currentRole, currentChannel, responseBuilder.ToString()));

                    channel.Writer.Complete();
                }
                else
                {
                    responseBuilder.Append("<|return|>");
                    history.Append(new(currentRole, currentChannel, responseBuilder.ToString()));

                    //run tool call
                    history.Append(new(GptOssRole.System, GptOssChannel.Commentary, "Response terminated unexpectedly."));
                    await channel.Writer.WriteAsync(new OllamaResponse { Response = "\n\nError: Response terminated unexpectedly.\n\n", Done = false, Channel = currentChannel.ToString() });

                    GetCompletion(null, channel);
                }
            }

        });

        return channel.Reader;
    }
}

