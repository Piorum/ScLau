using System.Text;
using System.Threading.Channels;
using ChatBackend.Builders;
using ChatBackend.Models;
using ChatBackend.Models.GptOss;

namespace ChatBackend;

public static class GptOss
{
    public static ChannelReader<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options)
    {
        Channel<ModelResponse> channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            var compiledPrompt = CompileChatHistory(history, options);
            var llmOptions = new ChatOptions()
            {
                ModelName = "gpt-oss:20b",
                Temperature = options.Temperature
            };

            var modelOutput = LLMProvider.StreamCompletionAsync(compiledPrompt, llmOptions);

            ChatState state = new()
            {
                History = history,
                Options = options,
                Channel = channel
            };
            StringBuilder messageBuilder = new();

            await foreach (var token in modelOutput.ReadAllAsync())
            {
                if (token is null) continue;

                if (state.TokenHandlers.TryGetValue(token, out var handler))
                {
                    await handler(state, messageBuilder);
                }
                else
                {
                    messageBuilder.Append(token);
                    if (state.IncomingMessage)
                    {
                        await channel.Writer.WriteAsync(new()
                        {
                            MessageId = state.CurrentMessageId,
                            ContentType = state.CurrentChannel == GptOssChannel.Final ? ContentType.Answer : ContentType.Reasoning,
                            ContentChunk = token,
                            IsDone = false
                        });
                    }
                }
            }

            await state.FinalizeConversation(messageBuilder);
        });

        return channel;
    }

    public static string CompileChatHistory(ChatHistory history, ChatOptions options)
    {

        return "";
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

        chat.TrailingPrompt(promptBuilder);
    }

    private record ChatState
    {
        required public ChatHistory History;
        required public ChatOptions Options;
        required public Channel<ModelResponse> Channel;

        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public bool IncomingRole { get; private set; } = false;
        public bool IncomingChannel { get; private set; } = false;
        public bool IncomingMessage { get; private set; } = false;

        private string? channelRaw = null;
        private string? functionName = null;
        public GptOssChannel CurrentChannel { get; private set; } = GptOssChannel.None;
        public GptOssRole CurrentRole { get; private set; } = GptOssRole.Assistant;

        public Guid CurrentMessageId;
        public ChatMessage CurrentChatMessage;

        public ChatState()
        {
            UpdateCurrentMessageId();
            CurrentChatMessage = new();
        }

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
                : GptOssRole.Assistant;

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
            UpdateHistory(new(CurrentRole, CurrentChannel, $"{sb}"));

            sb.Clear();

            return Task.CompletedTask;
        }

        public async Task FinalizeConversation(StringBuilder sb)
        {
            if (CurrentChannel != GptOssChannel.Commentary)
            {
                sb.Append($"<|return|>");
                UpdateHistory(new(CurrentRole, CurrentChannel, $"{sb}"));

                await Channel.Writer.WriteAsync(new()
                {
                    IsDone = true
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
                    ChatMessage toolCallMessage = new()
                    {
                        MessageId = CurrentMessageId,
                        Role = MessageRole.Tool,
                        Content = $"{sb}"
                    };
                    toolCallMessage.ExtendedProperties.Add("call", true);
                    toolCallMessage.ExtendedProperties.Add("valid_tool", false);
                    toolCallMessage.ExtendedProperties.Add("channel_raw", channelRaw ?? "commentary");

                    History.Messages.Add(toolCallMessage);
                    UpdateCurrentMessageId();

                    error = true;
                    errorMessage = "\nError: Malformed or invalid tool name.\n";
                }
                else
                {
                    ChatMessage toolCallMessage = new()
                    {
                        MessageId = CurrentMessageId,
                        Role = MessageRole.Tool,
                        Content = $"{sb}"
                    };
                    toolCallMessage.ExtendedProperties.Add("channel", functionName);
                    toolCallMessage.ExtendedProperties.Add("call", true);
                    toolCallMessage.ExtendedProperties.Add("valid_tool", true);
                    toolCallMessage.ExtendedProperties.Add("function_name", functionName);

                    History.Messages.Add(toolCallMessage);
                    UpdateCurrentMessageId();

                    var jsonString = $"{sb}";
                    var toolCallResult = await toolCall(jsonString);

                    switch (toolCallResult.Success)
                    {
                        case ToolCallSuccessFlag.Success:
                            ChatMessage toolResultMessage = new()
                            {
                                MessageId = CurrentMessageId,
                                Role = MessageRole.Tool,
                                Content = toolCallResult.Seralize()
                            };
                            toolCallMessage.ExtendedProperties.Add("call", false);
                            toolCallMessage.ExtendedProperties.Add("valid_tool", true);
                            toolCallMessage.ExtendedProperties.Add("function_name", functionName);

                            History.Messages.Add(toolCallMessage);
                            UpdateCurrentMessageId();

                            await Channel.Writer.WriteAsync(new()
                            {
                                ContentChunk = toolCallResult.Seralize(),
                                ContentType = ContentType.Reasoning,
                                MessageId = CurrentMessageId,
                                IsDone = false
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
                    UpdateHistory(new(GptOssRole.System, GptOssChannel.Commentary, errorMessage));

                    await Channel.Writer.WriteAsync(new()
                    {
                        ContentChunk = errorMessage,
                        ContentType = ContentType.Reasoning,
                        MessageId = CurrentMessageId,
                        IsDone = false
                    });
                }

                //continue chat loop returning error or tool call result
                var newChannel = ContinueChatAsync(History, Options);
                await foreach (var gptOssResponse in newChannel.ReadAllAsync())
                    await Channel.Writer.WriteAsync(gptOssResponse);

                Channel.Writer.Complete();
            }

        }

        private void UpdateHistory(GptOssChatChunk chunk)
        {
            ChatMessage message = new()
            {
                MessageId = CurrentMessageId,
                Role = MessageRole.Assistant,
                Content = chunk.Message
            };
            message.ExtendedProperties.Add("channel", $"{chunk.Channel.ToString().ToLower()}");
            History.Messages.Add(message);
            UpdateCurrentMessageId();
        }

        private void UpdateCurrentMessageId()
        {

            CurrentMessageId = Guid.NewGuid();
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

