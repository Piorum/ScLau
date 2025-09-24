using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using ChatBackend.Builders;
using ChatBackend.Models.GptOss;
using ChatBackend.Models.Ollama;

namespace ChatBackend;

public static class GptOss
{
    private static readonly ConcurrentDictionary<ulong, OllamaOptions> _chatOptions = [];
    private static readonly ConcurrentDictionary<ulong, GptOssChatBuilder> _chatHistory = [];

    public static ChannelReader<GptOssResponse> ContinueChat(ulong chatId, string? userPrompt = null)
    {
        Channel<GptOssResponse> channel = Channel.CreateUnbounded<GptOssResponse>();

        _ = Task.Run(async () =>
        {
            var history = GetChatHistory(chatId);
            if(userPrompt is not null)
                history.Append(new(GptOssRole.User, GptOssChannel.None, userPrompt));

            var options = GetChatOptions(chatId);

            var ollamaChannel = Channel.CreateUnbounded<OllamaResponse>();
            Ollama.GetCompletion(new() { Model = "gpt-oss:20b", Prompt = history.WithAssistantTrail(), Options = options }, ollamaChannel);

            ChatState state = new();
            StringBuilder messageBuilder = new();
            await foreach (var ollamaResponse in ollamaChannel.Reader.ReadAllAsync())
            {
                var token = ollamaResponse.Response;
                await Console.Out.WriteLineAsync($"{state} {token}");

                if (state.incomingMessage)
                    if (token == "<|end|>")
                    {
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (false, false, false);
                        history.Append(new(state.currentRole, state.currentChannel, messageBuilder.ToString()));
                        messageBuilder.Clear();
                    }
                    else
                    {
                        messageBuilder.Append(token);

                        var returnChunk = new GptOssResponse()
                        {
                            Response = token,
                            Channel = state.currentChannel.ToString()
                        };

                        await channel.Writer.WriteAsync(returnChunk);
                    }
                else if (state.incomingChannel)
                {
                    if (token == "<|message|>")
                    {
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (false, false, true);
                        var channelString = messageBuilder.ToString();
                        switch (channelString)
                        {
                            case "analysis":
                                state.currentChannel = GptOssChannel.Analysis;
                                break;
                            case "final":
                                state.currentChannel = GptOssChannel.Final;
                                break;
                            default:
                                if (channelString.Contains("commentary"))
                                {
                                    state.currentChannel = GptOssChannel.Commentary;
                                    state.channelRaw = channelString;
                                }
                                else
                                {
                                    state.currentChannel = GptOssChannel.None;
                                    await Console.Out.WriteLineAsync($"Unexpected Channel: \"{channelString}\"");
                                }
                                break;
                        }
                        messageBuilder.Clear();
                    }
                    else
                    {
                        messageBuilder.Append(token);
                    }

                }
                else if (state.incomingRole)
                {
                    if (token == "<|channel|>")
                    {
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (false, true, false);
                        var roleString = messageBuilder.ToString();
                        state.currentRole = roleString switch
                        {
                            "assistant" => GptOssRole.Assistant,
                            "developer" => GptOssRole.Developer,
                            "system" => GptOssRole.System,
                            "user" => GptOssRole.User,
                            _ => GptOssRole.None
                        };
                        messageBuilder.Clear();
                    }
                    else
                    {
                        messageBuilder.Append(token);
                    }
                }
                else
                {
                    if (token == "<|start|>")
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (true, false, false);
                    else if (token == "<|channel|>")
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (false, true, false);
                    else if (token == "<|message|>")
                        (state.incomingRole, state.incomingChannel, state.incomingMessage) = (false, false, true);
                    else
                        break;

                }

            }

            if (state.currentChannel != GptOssChannel.Commentary)
            {
                messageBuilder.Append("<|return|>");
                history.Append(new(state.currentRole, state.currentChannel, messageBuilder.ToString()));

                await channel.Writer.WriteAsync(new() { Response = "", Channel = GptOssChannel.Final.ToString(), Done = true });
                channel.Writer.Complete();
            }
            else
            {
                //Presume tool call was attempted?
                messageBuilder.Append("<|call|>");
                history.Append(new(state.currentRole, state.currentChannel, messageBuilder.ToString()));

                //Would need to parse json and tool call here and pass back response
                string errorMessage = "\nError: Unexpected response termination\n";
                history.Append(new(GptOssRole.System, GptOssChannel.Commentary, errorMessage));
                await channel.Writer.WriteAsync(new GptOssResponse { Response = errorMessage, Channel = state.currentChannel.ToString() });

                //Recursively continue chat
                var newChannel = ContinueChat(chatId);
                await foreach (var gptOssResponse in newChannel.ReadAllAsync())
                {
                    await channel.Writer.WriteAsync(gptOssResponse);
                }
            }


        });

        return channel;
    }

    public static GptOssChatBuilder GetChatHistory(ulong chatId)
    {
        if (!_chatHistory.TryGetValue(chatId, out var history))
        {
            history = new GptOssChatBuilder();
            SetPrompt(history);

            _chatHistory.TryAdd(chatId, history);
        }

        return history;
    }

    public static void SetPrompt(
        GptOssChatBuilder chat,
        string systemMessage = "You are a large language model (LLM).",
        string developerMessage = "Fulfill the request to the best of your abilities",
        GptOssReasoningLevel reasoningLevel = GptOssReasoningLevel.Low
    )
    {
        var promptBuilder = new GptOssPromptBuilder()
                .WithSystemMessage(systemMessage)
                .WithDeveloperInstructions(developerMessage)
                .WithReasoningLevel(reasoningLevel);

        chat.WithPrompt(promptBuilder);
    }

    public static OllamaOptions GetChatOptions(ulong chatId)
    {
        if (!_chatOptions.TryGetValue(chatId, out var options))
        {
            options = new();

            _chatOptions.TryAdd(chatId, options);
        }

        return options;
    }

    public static void SetOptions(ulong chatId, OllamaOptions options)
    {
        var existingOptions = GetChatOptions(chatId);
        _chatOptions.TryUpdate(chatId, options, existingOptions);
    }

    private record ChatState()
    {
        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public bool incomingRole = false;
        public bool incomingChannel = false;
        public bool incomingMessage = false;

        public string? channelRaw = null;
        public GptOssChannel currentChannel = GptOssChannel.None;
        public GptOssRole currentRole = GptOssRole.Assistant;
    }

}
