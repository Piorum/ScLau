using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend;

public class HarmonyFormatProvider(ILLMProvider llmProvider, IToolFactory toolFactory) : IChatProvider
{
    private readonly ILLMProvider _llmProvider = llmProvider;
    private readonly IToolFactory _toolFactory = toolFactory;

    public string Name { get; } = nameof(HarmonyFormatProvider);

    public IEnumerable<ProviderOptionDescriptor> ExtendedOptions { get; private set; } =
    [
        ExtendedOptionDescriptors.Model,
        ExtendedOptionDescriptors.MetaInformation,
        ExtendedOptionDescriptors.ReasoningLevel
    ];

    public IAsyncEnumerable<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options, CancellationToken cancellationToken = default)
    {
        Channel<ModelResponse> channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            bool isConversationDone = false;

            while (!isConversationDone)
            {
                bool continuation = history.Messages.LastOrDefault() is { Role: not MessageRole.User };
                var prompt = new HarmonyFormatHistoryBuilder(_toolFactory).WithHistory(history).WithOptions(options).ToString(continuation);

                var model = ExtendedOptionDescriptors.Model.GetValue<string>(options);

                var modelOutput = _llmProvider.StreamCompletionAsync($"{prompt}{HarmonyTokens.Start}{HarmonyRoles.Assistant}", model, options, cancellationToken: cancellationToken);

                ChatState state = new(_toolFactory, promptHasAssistantTrail: true)
                {
                    History = history,
                    Options = options,
                    Channel = channel
                };
                StringBuilder messageBuilder = new();

                try
                {
                    await foreach (var token in modelOutput)
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
                }
                catch (OperationCanceledException)
                {
                    isConversationDone = true;
                }
                finally
                {
                    var turnCompletedSuccesfully = await state.ProcessTurnCompletion(messageBuilder);

                    //Cancelled or turn is actually over
                    isConversationDone = isConversationDone || turnCompletedSuccesfully;
                }

            }

            await channel.Writer.WriteAsync(new() { IsDone = true });
            channel.Writer.Complete();
        }, CancellationToken.None);

        return channel.Reader.ReadAllAsync(CancellationToken.None);
    }

    private class ChatState
    {
        private readonly IToolFactory ToolFactory;

        required public ChatHistory History;
        required public ChatOptions Options;
        required public Channel<ModelResponse> Channel;

        public bool IncomingRole { get; private set; } = false;
        public bool IncomingMessage { get; private set; } = false;

        public string? CurrentChannel { get; private set; } = null;

        public Guid CurrentMessageId;
        public ContentType ContentType => CurrentChannel == HarmonyChannels.Final ? ContentType.Answer : ContentType.Reasoning;

        private ChatState(IToolFactory toolDiscoveryService)
        {
            ToolFactory = toolDiscoveryService;
            CurrentMessageId = Guid.NewGuid();
        }

        //These default values are based off the state given to the AI when prompt is passed with assistant trail ~"<|start|>assistant"
        public ChatState(IToolFactory toolDiscoveryService, bool promptHasAssistantTrail) : this(toolDiscoveryService)
        {
            if (promptHasAssistantTrail)
                IncomingRole = false;
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
                IncomingRole = false;

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
            //Partial content from an aborted request that doesn't contain message content we can just end.
            if(!IncomingMessage)
                return false;

            string[] nonToolCallChannelNames = [HarmonyChannels.Commentary, HarmonyChannels.Analysis, HarmonyChannels.Final];
            if (nonToolCallChannelNames.Contains(CurrentChannel))
            {
                UpdateHistory($"{sb}", HarmonyTokens.Return);
                //return conversation is over
                return true;
            }
            else
            {
                string toolName = "unknown";
                if (!string.IsNullOrEmpty(CurrentChannel))
                {
                    string prefix = "commentary to=functions.";
                    string suffix = "<|constrain|>json";

                    if (CurrentChannel.StartsWith(prefix) && CurrentChannel.EndsWith(suffix))
                        toolName = CurrentChannel[prefix.Length..^suffix.Length].Trim();
                }

                var callId = $"{Guid.NewGuid()}";
                var callArguments = sb.ToString();
                var toolCall = ChatMessage.CreateToolMessage(CurrentMessageId, new(Id: callId, ToolName: toolName, Content: callArguments, Result: false));
                History.Messages.Add(toolCall);

                //Handle tool call
                ToolResult callResult;
                try
                {
                    using JsonDocument document = JsonDocument.Parse(callArguments);
                    callResult = await ToolFactory.ExecuteTool(toolName, document.RootElement);
                }
                catch(JsonException)
                {
                    callResult = ToolResult.Failure(ToolResultType.MalformedArguments, "Arguments could not be parsed into JSON.");
                }

                Guid messageId = Guid.NewGuid();
                string content;
                if (callResult.ResultType == ToolResultType.Success)
                {
                    content = callResult.Result!;
                    var toolReturn = ChatMessage.CreateToolMessage(messageId, new(Id: callId, ToolName: toolName, Content: content, Result: true));
                    History.Messages.Add(toolReturn);
                }
                else
                {
                    content = callResult.ResultType switch
                    {
                        ToolResultType.MalformedToolName => callResult.Error ?? $"Tool name \"{toolName}\" was incorrect, not a tool, or couldn't be parsed.",
                        ToolResultType.MalformedArguments => callResult.Error ?? $"Arguments for \"{toolName}\" were incorrect or couldn't be parsed.",
                        ToolResultType.ExecutionError => callResult.Error ?? "An error occured during execution of the tool.",
                        _ => "Unhandled tool result error type.",
                    };

                    var errorReturn = ChatMessage.CreateSystemMessage(messageId, content, ContentType.Reasoning);
                    errorReturn.ExtendedProperties.Add(ExtendedMessagePropertyKeys.Channel, HarmonyChannels.Commentary);
                    History.Messages.Add(errorReturn);

                    await Console.Out.WriteLineAsync($"Tool call return error.\n\"{toolName}\" :: \"{callArguments}\"\n\n{callResult.Error}");
                }

                //Output result to frontend
                await Channel.Writer.WriteAsync
                (
                    new()
                    {
                        ContentChunk = content,
                        ContentType = ContentType.Reasoning,
                        MessageId = messageId,
                        IsDone = false
                    }
                );

                //return false, conversation is not over
                return false;
            }

        }

        private void UpdateHistory(string content, string? endToken = null)
        {
            var message = ChatMessage.CreateAssistantMessage(CurrentMessageId, content, ContentType);

            if (CurrentChannel is not null)
                message.ExtendedProperties.Add(ExtendedMessagePropertyKeys.Channel, CurrentChannel);
            if (endToken is not null)
                message.ExtendedProperties.Add(ExtendedMessagePropertyKeys.EndToken, endToken);

            History.Messages.Add(message);

            CurrentChannel = null;
            CurrentMessageId = Guid.NewGuid();
        }

    }

    private class HarmonyFormatHistoryBuilder(IToolFactory toolFactory)
    {
        private readonly IToolFactory ToolFactory = toolFactory;

        private ChatHistory? history;
        private ChatOptions? options;

        private bool ToolsEnabled => options?.EnabledTools is { Count: > 0};

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

        private void Compile(bool continuation = false)
        {
            sb.Clear();

            if (options is not null)
            {
                AppendSystemMessage();
                AppendDeveloperMessage();
            }

            if (history is not null)
            {
                AppendMessageHistory(continuation);
            }

        }

        private void AppendSystemMessage()
        {
            StringBuilder systemMessage = new();
            string reasoningLevel = ExtendedOptionDescriptors.ReasoningLevel.GetValue<string>(options!);
            string metaInformation = ExtendedOptionDescriptors.MetaInformation.GetValue<string>(options!);

            systemMessage.Append($"{metaInformation}\nKnowledge cutoff: 2024-06\nCurrent date: {DateTime.Now:yyyy-MM-dd}\n\nReasoning: {reasoningLevel}\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message.");
            if (ToolsEnabled)
                systemMessage.Append("\nCalls to these tools must go to the commentary channel: 'functions'.");

            AppendMessage(HarmonyRoles.System, systemMessage.ToString());

        }
        private void AppendDeveloperMessage()
        {
            StringBuilder developerMessage = new();
            
            developerMessage.Append($"# Instructions\n{options!.SystemMessage}\n");
            if (ToolsEnabled)
                AppendTools(developerMessage);

            AppendMessage(HarmonyRoles.Developer, developerMessage.ToString());

        }

        private void AppendTools(StringBuilder developerMessage)
        {
            developerMessage.Append("# Tools\n## functions\nnamespace functions {\n");

            foreach (var tool in options!.EnabledTools!)
            {
                var toolInfo = ToolFactory.GetToolInfo(tool);
                if (toolInfo is null) continue;

                AppendTool(toolInfo, developerMessage);
            }

            developerMessage.Append("\n} // namespace functions");
        }

        private static void AppendTool(ToolInfo toolInfo, StringBuilder developerMessage)
        {
            developerMessage.Append($"// {toolInfo.Description}\ntype {toolInfo.Name} = (");

            if (toolInfo.Parameters.Count > 0)
            {
                developerMessage.Append("_: {");
                foreach (var parameter in toolInfo.Parameters)
                {
                    developerMessage.Append($"\n// {parameter.Description}\n{parameter.Name}{(parameter.IsRequired ? "" : "?")}: {GetParameterType(parameter)},");

                    if (parameter.DefaultValue is not null)
                        developerMessage.Append($" // default: {parameter.DefaultValue.ToString()?.ToLower()}");
                }
                developerMessage.Append("\n}");
            }

            developerMessage.Append(") => any;");
        }

        private static string GetParameterType(ToolParameterInfo parameter) =>
            parameter.EnumValues?.Any() == true
                ? string.Join(" | ", parameter.EnumValues.Select(e => $"\"{e.ToLower()}\""))
                : GetTypeType(parameter.Type);

        private static string GetTypeType(Type type) =>
            type switch
            {
                Type t when t == typeof(string) => "string",
                Type t when t == typeof(int) ||
                    t == typeof(double) ||
                    t == typeof(float) ||
                    t == typeof(decimal) ||
                    t == typeof(long) => "number",
                Type t when t == typeof(bool) => "boolean",
                Type t when t.IsArray => $"{GetTypeType(type.GetElementType()!)}[]",
                Type t when typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string) => $"{GetTypeType(t.IsGenericType ? t.GetGenericArguments()[0] : typeof(object))}[]",
                _ => "any"
            };
        
        private void AppendMessageHistory(bool continuation)
        {
            for(var i = 0; i < history!.Messages.Count; i++)
            {
                var message = history.Messages[i];
                if (message.Role == MessageRole.Tool)
                {
                    AppendToolMessage(message.ToolContext!);
                    continue;
                }

                string roleText = message.Role switch
                {
                    MessageRole.User => HarmonyRoles.User,
                    MessageRole.Assistant => HarmonyRoles.Assistant,
                    MessageRole.System => HarmonyRoles.System,
                    _ => throw new UnreachableException($"Message had unexpected role value provided: {message.Role}")
                };

                string? channelText = GetExtendedProperty(message, ExtendedMessagePropertyKeys.Channel);

                string? endToken = (continuation && i == history.Messages.Count - 1)
                    ? "" //blank end token if continuation is true and it's the last message
                    : GetExtendedProperty(message, ExtendedMessagePropertyKeys.EndToken);

                AppendMessage(roleText, message.Content!, channelText, endToken);
            }
        }

        private void AppendMessage(string roleText, string messageText, string? channelText = null, string? endToken = null)
        {
            sb.Append($"{HarmonyTokens.Start}{roleText}");
            if (channelText is not null)
                sb.Append($"{HarmonyTokens.Channel}{channelText}");
            sb.Append($"{HarmonyTokens.Message}{messageText}");
            sb.Append(endToken ?? HarmonyTokens.End);
        }

        private void AppendToolMessage(ToolContext toolContext)
        {
            sb.Append($"{HarmonyTokens.Start}{(toolContext.Result ? $"functions.{toolContext.ToolName} to=assistant" : HarmonyRoles.Assistant)}");
            sb.Append($"{HarmonyTokens.Channel}{HarmonyChannels.Commentary}{(toolContext.Result ? "" : $" to=functions.{toolContext.ToolName} <|constrain|>json")}");
            sb.Append($"{HarmonyTokens.Message}{toolContext.Content}");
            sb.Append(toolContext.Result ? HarmonyTokens.End : HarmonyTokens.Call);
        }

        private static string? GetExtendedProperty(ChatMessage message, string key) =>
            message.ExtendedProperties.TryGetValue(key, out var val) ? val as string : null;

        public override string ToString()
        {
            Compile();
            return sb.ToString();
        }

        public string ToString(bool continuation)
        {
            Compile(continuation);
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

            Type = ProviderOptionType.String,

            DefaultValue = "gpt-oss:20b"
        };

        public readonly static ProviderOptionDescriptor MetaInformation = new()
        {
            Name = "Meta Information",
            Key = "meta_information",
            Description = "Meta information to tell the model.",

            Type = ProviderOptionType.String,

            DefaultValue = "You are a large language model."
        };

        public readonly static ProviderOptionDescriptor ReasoningLevel = new()
        {
            Name = "Reasoning Level",
            Key = "reasoning_level",
            Description = "Level of reasoning effort the model should use.",

            Type = ProviderOptionType.Enum,

            DefaultValue = "low",

            AllowedValues = ["low", "medium", "high"]
        };
    }

    private static class ExtendedMessagePropertyKeys
    {
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