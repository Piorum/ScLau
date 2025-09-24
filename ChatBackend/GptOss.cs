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
            if (userPrompt is not null)
                history.Append(new(GptOssRole.User, GptOssChannel.None, userPrompt));

            var options = GetChatOptions(chatId);

            var ollamaChannel = Channel.CreateUnbounded<OllamaResponse>();
            Ollama.GetCompletion(new() { Model = "gpt-oss:20b", Prompt = history.WithAssistantTrail(), Options = options }, ollamaChannel);

            ChatState state = new()
            {
                History = history,
                Channel = channel
            };
            StringBuilder messageBuilder = new();

            await foreach (var ollamaResponse in ollamaChannel.Reader.ReadAllAsync())
            {
                var token = ollamaResponse.Response;
                if (token is null) continue;
                await Console.Out.WriteLineAsync($"{state} {token}");

                if (state.TokenHandlers.TryGetValue(token, out var handler))
                {
                    await handler(state, messageBuilder);
                }
                else
                {
                    messageBuilder.Append(token);
                    if (state.IncomingMessage)
                    {
                        await channel.Writer.WriteAsync(new GptOssResponse
                        {
                            Response = token,
                            Channel = $"{state.CurrentChannel}"
                        });
                    }
                }
            }

            await state.FinalizeConversation(messageBuilder, chatId);
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
        required public GptOssChatBuilder History;
        required public Channel<GptOssResponse> Channel;

        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public bool IncomingRole { get; private set; } = false;
        public bool IncomingChannel { get; private set; } = false;
        public bool IncomingMessage { get; private set; } = false;

        private string? channelRaw = null;
        private string? functionName = null;
        public GptOssChannel CurrentChannel { get; private set; } = GptOssChannel.None;
        public GptOssRole CurrentRole { get; private set; } = GptOssRole.Assistant;

        public readonly Dictionary<string, Func<ChatState, StringBuilder, Task>> TokenHandlers =
        new()
        {
            ["<|start|>"] = async (s, b) => await Task.Run(() => s.SetState(role: true)),
            ["<|channel|>"] = async (s, b) => await s.CommitRole(b),
            ["<|message|>"] = async (s, b) => await s.CommitChannel(b),
            ["<|end|>"] = async (s, b) => await s.CommitMessage(b),

        };

        private void SetState(bool role = false, bool channel = false, bool message = false)
            => (IncomingRole, IncomingChannel, IncomingMessage) = (role, channel, message);

        private Task CommitRole(StringBuilder sb)
        {
            SetState(channel: true);
            var roleString = $"{sb}";
            CurrentRole = Enum.TryParse<GptOssRole>(roleString, true, out var role)
                ? role
                : GptOssRole.None;

            sb.Clear();

            return Task.CompletedTask;
        }
        private async Task CommitChannel(StringBuilder sb)
        {
            SetState(message: true);
            var channelString = $"{sb}";
            CurrentChannel = channelString switch
            {
                "analysis" => GptOssChannel.Analysis,
                "final" => GptOssChannel.Final,
                _ when channelString.StartsWith("commentary") =>
                    (
                        //Extracts function name or sets to null if malformed/not a function
                        //(shouldn't impact preamble commentary will just set to null)
                        channelRaw = channelString,
                        functionName =
                            channelString is not null
                            && channelString.StartsWith("commentary to=functions.")
                            && channelString.EndsWith("<|constrain|>json")
                                ? channelString?["commentary to=functions.".Length..^"<|constrain|>json".Length].Trim()
                                : null,
                        GptOssChannel.Commentary
                    ).Commentary,
                _ => GptOssChannel.None
            };
            if (CurrentChannel == GptOssChannel.None)
                await Console.Out.WriteLineAsync($"Unexpected Channel: \"{channelString}\"");

            sb.Clear();
        }
        private Task CommitMessage(StringBuilder sb)
        {
            SetState();
            History.Append(new(CurrentRole, CurrentChannel, $"{sb}"));

            sb.Clear();

            return Task.CompletedTask;
        }

        public async Task FinalizeConversation(StringBuilder sb, ulong chatId)
        {
            if (CurrentChannel != GptOssChannel.Commentary)
            {
                sb.Append($"<|return|>");
                History.Append(new(CurrentRole, CurrentChannel, $"{sb}"));

                await Channel.Writer.WriteAsync(new()
                {
                    Response = "",
                    Channel = $"{GptOssChannel.Final}",
                    Done = true
                });
                Channel.Writer.Complete();
            }
            else
            {
                //Example non-functional
                Dictionary<string, Func<string, Task<IToolCallResult>>> availableTools = [];

                bool error = false;
                string errorMessage = "";
                if (functionName is null || !availableTools.TryGetValue(functionName, out var toolCall))
                {
                    History.AppendMalformedToolCall(channelRaw ?? "commentary", $"{sb}");

                    error = true;
                    errorMessage = "\nError: Malformed or invalid tool name.\n";
                }
                else
                {
                    History.AppendToolCall(functionName, $"{sb}");

                    var jsonString = $"{sb}";
                    var toolCallResult = await toolCall(jsonString);

                    switch (toolCallResult.Success)
                    {
                        case ToolCallSuccessFlag.Success:
                            History.AppendToolResult(functionName, toolCallResult);

                            await Channel.Writer.WriteAsync(new GptOssResponse
                            {
                                Response = toolCallResult.Seralize(),
                                Channel = $"{GptOssChannel.Commentary}"
                            });

                            break;
                        case ToolCallSuccessFlag.Failed:
                            error = true;
                            errorMessage = "\nError: Tool call failed but request was valid.\n";
                            break;
                        case ToolCallSuccessFlag.Malformed:
                            error = true;
                            errorMessage = "\nError: Tool call arguments were malformed.\n";
                            break;
                    }
                }

                if (error)
                {
                    History.Append(new(GptOssRole.System, GptOssChannel.Commentary, errorMessage));

                    await Channel.Writer.WriteAsync(new GptOssResponse
                    {
                        Response = errorMessage,
                        Channel = CurrentChannel.ToString()
                    });
                }

                //continue chat loop returning error or tool call result
                var newChannel = ContinueChat(chatId);
                await foreach (var gptOssResponse in newChannel.ReadAllAsync())
                    await Channel.Writer.WriteAsync(gptOssResponse);
            }

        }

    }

    public interface IToolCallResult
    {
        ToolCallSuccessFlag Success { get; }
        string Seralize();
    }

    public enum ToolCallSuccessFlag
    {
        Success,
        Failed,
        Malformed
    }
}

