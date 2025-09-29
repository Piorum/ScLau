using System.Text;
using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend;

public class GptOss : IChatGenerator
{
    public ChannelReader<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options)
    {
        Channel<ModelResponse> channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            bool isConversationDone = false;

            while (!isConversationDone)
            {
                var prompt = new GptOssHistoryBuilder().WithHistory(history).WithOptions(options).ToString();
                var modelOutput = LLMProvider.StreamCompletionAsync($"{prompt}<|start|>assistant", options);

                ChatState state = new(promptHasAssistantTrail: true)
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
                                ContentType = state.ContentType,
                                ContentChunk = token,
                                IsDone = false
                            });
                        }
                    }
                }

                isConversationDone = await state.ProcessTurnCompletion(messageBuilder);
            }

            await channel.Writer.WriteAsync(new() { IsDone = true });
            channel.Writer.Complete();

            await Console.Out.WriteLineAsync($"{new GptOssHistoryBuilder().WithHistory(history).WithOptions(options)}");
        });

        return channel;
    }

    private class ChatState
    {
        required public ChatHistory History;
        required public ChatOptions Options;
        required public Channel<ModelResponse> Channel;

        public bool IncomingRole { get; private set; } = false;
        public bool IncomingMessage { get; private set; } = false;

        public string? CurrentChannel { get; private set; } = null;
        public string? CurrentRole { get; private set; } = null;

        public Guid CurrentMessageId;
        public ContentType ContentType => CurrentChannel == "final" ? ContentType.Answer : ContentType.Reasoning;

        private ChatState()
        {
            CurrentMessageId = Guid.NewGuid();
        }

        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public ChatState(bool promptHasAssistantTrail) : this()
        {
            if (promptHasAssistantTrail)
            {
                CurrentRole = "assistant";
                IncomingRole = false;
            }
        }

        public readonly Dictionary<string, Func<ChatState, StringBuilder, Task>> TokenHandlers =
        new()
        {
            ["<|start|>"] = async (s, b) => await Task.Run(() => s.IncomingRole = true),
            ["<|channel|>"] = async (s, b) => await s.ProcessRoleContent(b),
            ["<|message|>"] = async (s, b) => await s.ProcessChannelContent(b),
            ["<|end|>"] = async (s, b) => await s.ProcessMessageContent(b),

        };

        private Task ProcessRoleContent(StringBuilder sb)
        {
            if (IncomingRole)
            {
                CurrentRole = $"{sb}";
                IncomingRole = false;
            }

            sb.Clear();
            return Task.CompletedTask;
        }
        private Task ProcessChannelContent(StringBuilder sb)
        {
            IncomingMessage = true;
            CurrentChannel = $"{sb}";

            sb.Clear();
            return Task.CompletedTask;
        }
        private Task ProcessMessageContent(StringBuilder sb)
        {
            IncomingMessage = false;
            UpdateHistory($"{sb}");

            sb.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Finalizes Assistant Turn
        /// </summary>
        /// <param name="sb"></param>
        /// <returns>Returns 'true' if assistant has yielded</returns>
        public async Task<bool> ProcessTurnCompletion(StringBuilder sb)
        {
            //This logic is terrible but not sure what else to do.
            string[] nonToolCallChannelNames = ["commentary", "analysis", "final"];
            if (nonToolCallChannelNames.Contains(CurrentChannel))
            {
                UpdateHistory($"{sb}", "return");
                //return conversation is over
                return true;
            }
            else
            {
                UpdateHistory($"{sb}", "call");

                //Handle tool call
                //Append tool call reply to history
                //Output to frontend?
                string errorMessage = "\nError: Tools are unavailable.\n";
                Guid messageId = Guid.NewGuid();
                ChatMessage callReturn = new()
                {
                    MessageId = messageId,
                    Role = MessageRole.Tool,
                    Content = errorMessage
                };
                callReturn.ExtendedProperties.Add("role", "system");
                callReturn.ExtendedProperties.Add("channel", "commentary");
                History.Messages.Add(callReturn);
                await Channel.Writer.WriteAsync
                (
                    new()
                    {
                        ContentChunk = errorMessage,
                        ContentType = ContentType.Reasoning,
                        MessageId = messageId,
                        IsDone = false
                    }
                );

                //return conversation is not over
                return false;
            }

        }

        private void UpdateHistory(string content, string? endToken = null)
        {
            ChatMessage message = new()
            {
                MessageId = CurrentMessageId,
                Role = Enum.TryParse<MessageRole>(CurrentRole, true, out var currentRole) ? currentRole : MessageRole.Tool,
                Content = content
            };

            if(CurrentChannel is not null)
                message.ExtendedProperties.Add("channel", CurrentChannel);
            if (endToken is not null)
                message.ExtendedProperties.Add("end_token", endToken);

            History.Messages.Add(message);

            CurrentRole = null;
            CurrentChannel = null;
            CurrentMessageId = Guid.NewGuid();
        }

    }
}

public class GptOssHistoryBuilder
{
    private ChatHistory? history;
    private ChatOptions? options;

    private readonly StringBuilder sb = new();

    public GptOssHistoryBuilder WithHistory(ChatHistory history)
    {
        this.history = history;
        return this;
    }
    public GptOssHistoryBuilder WithOptions(ChatOptions options)
    {
        this.options = options;
        return this;
    }

    private void Compile()
    {
        sb.Clear();

        if (options is not null)
        {
            string reasoningLevel = GetProperty(options, "reasoning_level") ?? "low";

            string? developerMessage = GetProperty(options, "developer_message");

            Append("system", $"{options.SystemMessage}\nKnowledge cutoff: 2024-06\nCurrent date: {DateTime.Now:yyyy-MM-dd}\n\nReasoning: {reasoningLevel}\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message.");
            if (developerMessage is not null)
                Append("developer", $"# Instructions\n\n{developerMessage}\n\n");
        }

        if (history is not null)
        {
            foreach (var message in history.Messages)
            {
                string roleText = message.Role switch
                {
                    MessageRole.User => "user",
                    MessageRole.Assistant => "assistant",
                    MessageRole.Tool => GetToolRoleText(message),
                    _ => throw new System.ComponentModel.InvalidEnumArgumentException(message.Role.ToString(), (int)message.Role, typeof(MessageRole))
                };

                string? channelText = GetProperty(message, "channel");

                string? endTokenText = GetProperty(message, "end_token");

                Append(roleText, message.Content, channelText, endTokenText);
            }
        }

    }

    private static string GetToolRoleText(ChatMessage message)
    {
        string roleText = GetProperty(message, "role") ?? "system";

        return roleText;
    }

    private static string? GetProperty(IExtensibleProperties obj, string key) =>
        obj.ExtendedProperties.TryGetValue(key, out var val) ? val as string : null;

    private void Append(string roleText, string messageText, string? channelText = null, string? endTokenText = null)
    {
        sb.Append($"<|start|>{roleText}");
        if (channelText is not null)
            sb.Append($"<|channel|>{channelText}");
        sb.Append($"<|message|>{messageText}");
        sb.Append($"<|{endTokenText ?? "end"}|>");
    }

    public override string ToString()
    {
        Compile();
        return sb.ToString();
    }
}