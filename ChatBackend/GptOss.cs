using System.Text;
using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend;

public class HarmonyFormatProvider : IChatGenerator
{
    public string Name { get; } = nameof(HarmonyFormatProvider);

    public IEnumerable<ProviderOptionDescriptor> ExtendedOptions { get; private set; } =
    [
        ExtendedOptionDescriptors.Model,
        ExtendedOptionDescriptors.MetaInformation,
        ExtendedOptionDescriptors.ReasoningLevel
    ];

    public ChannelReader<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options)
    {
        Channel<ModelResponse> channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            bool isConversationDone = false;

            while (!isConversationDone)
            {
                var prompt = new HarmonyFormatHistoryBuilder().WithHistory(history).WithOptions(options).ToString();

                var model = ExtendedOptionDescriptors.Model.GetValue<string>(options);

                var modelOutput = LLMProvider.StreamCompletionAsync($"{prompt}{HarmonyTokens.Start}{HarmonyRoles.Assistant}", model, options);

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
        public ContentType ContentType => CurrentChannel == HarmonyChannels.Final ? ContentType.Answer : ContentType.Reasoning;

        private ChatState()
        {
            CurrentMessageId = Guid.NewGuid();
        }

        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public ChatState(bool promptHasAssistantTrail) : this()
        {
            if (promptHasAssistantTrail)
            {
                CurrentRole = HarmonyRoles.Assistant;
                IncomingRole = false;
            }
        }

        public readonly Dictionary<string, Func<ChatState, StringBuilder, Task>> TokenHandlers =
        new()
        {
            [HarmonyTokens.Start] = async (s, b) => await Task.Run(() => s.IncomingRole = true),
            [HarmonyTokens.Channel] = async (s, b) => await s.ProcessRoleContent(b),
            [HarmonyTokens.Message] = async (s, b) => await s.ProcessChannelContent(b),
            [HarmonyTokens.End] = async (s, b) => await s.ProcessMessageContent(b),

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
            string[] nonToolCallChannelNames = [HarmonyChannels.Commentary, HarmonyChannels.Analysis, HarmonyChannels.Final];
            if (nonToolCallChannelNames.Contains(CurrentChannel))
            {
                UpdateHistory($"{sb}", HarmonyTokens.Return);
                //return conversation is over
                return true;
            }
            else
            {
                UpdateHistory($"{sb}", HarmonyTokens.Call);

                //Handle tool call
                //Append tool call reply to history
                //Output to frontend?
                string errorMessage = "Error: Tools are unavailable.";
                Guid messageId = Guid.NewGuid();
                ChatMessage callReturn = new()
                {
                    MessageId = messageId,
                    Role = MessageRole.Tool,
                    Content = errorMessage,
                    ContentType = ContentType.Reasoning
                };
                callReturn.ExtendedProperties.Add(ExtendedMessagePropertyKeys.Role, HarmonyRoles.System);
                callReturn.ExtendedProperties.Add(ExtendedMessagePropertyKeys.Channel, HarmonyChannels.Commentary);
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

            if (CurrentChannel is not null)
            {
                message.ExtendedProperties.Add(ExtendedMessagePropertyKeys.Channel, CurrentChannel);
                if (CurrentChannel != HarmonyChannels.Final)
                    message.ContentType = ContentType.Reasoning;
            }
            if (endToken is not null)
                message.ExtendedProperties.Add(ExtendedMessagePropertyKeys.EndToken, endToken);

            History.Messages.Add(message);

            CurrentRole = null;
            CurrentChannel = null;
            CurrentMessageId = Guid.NewGuid();
        }

    }

    private class HarmonyFormatHistoryBuilder
    {
        private ChatHistory? history;
        private ChatOptions? options;

        private readonly StringBuilder sb = new();

        public HarmonyFormatHistoryBuilder WithHistory(ChatHistory history)
        {
            this.history = history;
            return this;
        }
        public HarmonyFormatHistoryBuilder WithOptions(ChatOptions options)
        {
            this.options = options;
            return this;
        }

        private void Compile()
        {
            sb.Clear();

            if (options is not null)
            {
                string reasoningLevel = ExtendedOptionDescriptors.ReasoningLevel.GetValue<string>(options);

                string metaInformation = ExtendedOptionDescriptors.MetaInformation.GetValue<string>(options);

                Append(HarmonyRoles.System, $"{metaInformation}\nKnowledge cutoff: 2024-06\nCurrent date: {DateTime.Now:yyyy-MM-dd}\n\nReasoning: {reasoningLevel}\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message.");
                Append(HarmonyRoles.Developer, $"# Instructions\n\n{options.SystemMessage}\n\n");
            }

            if (history is not null)
            {
                foreach (var message in history.Messages)
                {
                    string roleText = message.Role switch
                    {
                        MessageRole.User => HarmonyRoles.User,
                        MessageRole.Assistant => HarmonyRoles.Assistant,
                        MessageRole.Tool => GetToolRoleText(message),
                        _ => throw new System.ComponentModel.InvalidEnumArgumentException(message.Role.ToString(), (int)message.Role, typeof(MessageRole))
                    };

                    string? channelText = GetProperty(message, ExtendedMessagePropertyKeys.Channel);

                    string? endToken = GetProperty(message, ExtendedMessagePropertyKeys.EndToken);

                    Append(roleText, message.Content, channelText, endToken);
                }
            }

        }

        private static string GetToolRoleText(ChatMessage message)
        {
            string roleText = GetProperty(message, ExtendedMessagePropertyKeys.Role) ?? HarmonyRoles.System;

            return roleText;
        }

        private static string? GetProperty(ChatMessage message, string key) =>
            message.ExtendedProperties.TryGetValue(key, out var val) ? val as string : null;

        private void Append(string roleText, string messageText, string? channelText = null, string? endToken = null)
        {
            sb.Append($"{HarmonyTokens.Start}{roleText}");
            if (channelText is not null)
                sb.Append($"{HarmonyTokens.Channel}{channelText}");
            sb.Append($"{HarmonyTokens.Message}{messageText}");
            sb.Append($"{endToken ?? HarmonyTokens.End}");
        }

        public override string ToString()
        {
            Compile();
            return sb.ToString();
        }
    }

    private static class ExtendedOptionDescriptors
    {
        public readonly static ProviderOptionDescriptor Model = new()
        {
            Name = "Model Name",
            Key = "model_name",
            Description = "Full Ollama model name and tag.",

            Type = OptionType.String,

            DefaultValue = "gpt-oss:20b"
        };

        public readonly static ProviderOptionDescriptor MetaInformation = new()
        {
            Name = "Meta Information",
            Key = "meta_information",
            Description = "Meta information to tell the model.",

            Type = OptionType.String,

            DefaultValue = "You are a large language model."
        };

        public readonly static ProviderOptionDescriptor ReasoningLevel = new()
        {
            Name = "Reasoning Level",
            Key = "reasoning_level",
            Description = "Level of reasoning effort the model should use.",

            Type = OptionType.Enum,

            DefaultValue = "low",

            AllowedValues = ["low", "medium", "high"]
        };
    }

    private static class ExtendedMessagePropertyKeys
    {
        public const string Role = "role";
        public const string Channel = "channel";
        public const string EndToken = "end_token";
    }

    private static class HarmonyRoles
    {
        public const string System = "system";
        public const string Developer = "developer";
        public const string Assistant = "assistant";
        public const string User = "user";
    }

    private static class HarmonyChannels
    {
        public const string Analysis = "analysis";
        public const string Commentary = "commentary";
        public const string Final = "final";
    }

    private static class HarmonyTokens
    {
        public const string Start = "<|start|>";
        public const string Message = "<|message|>";
        public const string Channel = "<|channel|>";
        public const string End = "<|end|>";
        public const string Call = "<|call|>";
        public const string Return = "<|return|>";
    }
}