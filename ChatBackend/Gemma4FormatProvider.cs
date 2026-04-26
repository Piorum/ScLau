using System.Text;
using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend;

public class Gemma4FormatProvider(ILLMProvider llmProvider, IToolFactory toolFactory) : IChatProvider
{
    private readonly ILLMProvider _llmProvider = llmProvider;
    private readonly IToolFactory _toolFactory = toolFactory;

    public string Name { get; } = nameof(Gemma4FormatProvider);

    public IEnumerable<ProviderOptionDescriptor> ExtendedOptions { get; private set; } =
    [
        ExtendedOptionDescriptors.Model,
        ExtendedOptionDescriptors.Thinking,
    ];

    public IAsyncEnumerable<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options, CancellationToken cancellationToken = default)
    {
        Channel<ModelResponse> channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            options.ModelOptions ??= new();
            options.ModelOptions.Stop = ["<turn|>", "<tool_call|>"];
            var prompt = new Gemma4FormatHistoryBuilder(_toolFactory).WithHistory(history).WithOptions(options).ToString();

            var model = ExtendedOptionDescriptors.Model.GetValue<string>(options);
            var thinking = ExtendedOptionDescriptors.Thinking.GetValue<bool>(options);

            var modelOutput = _llmProvider.StreamCompletionAsync($"{prompt}{Gemma4Tokens.Initiator(thinking)}", model, options, cancellationToken: cancellationToken);
            
            StringBuilder sb = new();
            Guid guid = Guid.NewGuid();

            //Frontend expects first token to be Reasoning, just send one when thinking is disabled for now.
            if(!thinking)
                await channel.Writer.WriteAsync(new()
                {
                    MessageId = Guid.NewGuid(), 
                    ContentType = ContentType.Reasoning, 
                    ContentChunk = "Thinking Disabled", 
                    IsDone = false 
                });

            await foreach (var token in modelOutput)
            {
                sb.Append(token);
                await channel.Writer.WriteAsync(new()
                {
                    MessageId = guid,
                    ContentType = ContentType.Answer,
                    ContentChunk = token,
                    IsDone = false
                });
            }

            history.Messages.Add(ChatMessage.CreateAssistantMessage(guid, sb.ToString(), ContentType.Answer));

            await channel.Writer.WriteAsync(new() { IsDone = true });
            channel.Writer.Complete();

        }, CancellationToken.None);

        return channel.Reader.ReadAllAsync(CancellationToken.None);
    }

    private class Gemma4FormatHistoryBuilder(IToolFactory toolFactory)
    {
        private readonly IToolFactory ToolFactory = toolFactory;

        private ChatHistory? history;
        private ChatOptions? options;

        private bool ToolsEnabled => options?.EnabledTools is { Count: > 0};

        private readonly StringBuilder sb = new();

        public Gemma4FormatHistoryBuilder WithHistory(ChatHistory history)
        {
            this.history = history;
            return this;
        }
        public Gemma4FormatHistoryBuilder WithOptions(ChatOptions options)
        {
            this.options = options;
            return this;
        }

        private void Compile()
        {
            sb.Clear();

            if (options is not null)
                AppendSystemMessage();

            if (history is not null)
                AppendMessageHistory();

        }

        private void AppendSystemMessage()
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.Turn))
                .AppendLine(Gemma4Roles.System);
            
            if(ExtendedOptionDescriptors.Thinking.GetValue<bool>(options!))
                sb.Append(Gemma4Tokens.Thinking);
        

            sb.Append(options!.SystemMessage);

            if(ToolsEnabled)
                AppendTools();

            sb.AppendLine(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.Turn));

        }

        private void AppendTools()
        {
            foreach (var tool in options!.EnabledTools!)
            {
                var toolInfo = ToolFactory.GetToolInfo(tool);
                if (toolInfo is null) continue;

                AppendTool(toolInfo);
            }
        }

        private static void AppendTool(ToolInfo toolInfo)
        {
            //Append tool declaration
        }
        
        private void AppendMessageHistory()
        {
            var turns = history!.Messages.GroupAdjacent(m =>
                m.Role == MessageRole.Assistant || m.Role == MessageRole.Tool
                    ? Gemma4Roles.Model
                    : m.Role switch
                    {
                        MessageRole.User => Gemma4Roles.User,
                        MessageRole.System => Gemma4Roles.System,
                        _ => throw new ArgumentOutOfRangeException(nameof(m.Role), m.Role, "Unexpected enum value provided.")
                    });

            foreach (var turn in turns)
                AppendTurn(turn);
        }

        private void AppendTurn(List<ChatMessage> turnMessages)
        {
            var firstMessage = turnMessages.First();
            var role = firstMessage.Role;

            if(role == MessageRole.User || role == MessageRole.System)
                AppendSimpleMessage(role, firstMessage.Content ?? "");
            else
                AppendModelTurn(turnMessages);
        }

        private void AppendSimpleMessage(MessageRole role, string messageContent)
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.Turn))
                .AppendLine(role switch
                {
                    MessageRole.User => Gemma4Roles.User,
                    MessageRole.System => Gemma4Roles.System,
                    _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unexpected enum value provided.")
                })
                .Append(messageContent)
                .AppendLine(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.Turn));
        }

        private void AppendModelTurn(List<ChatMessage> turnMessages)
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.Turn))
                .AppendLine(Gemma4Roles.Model);

            var functionCallingTurn = turnMessages.Any(e => e.Role == MessageRole.Tool);

            foreach(var message in turnMessages)
                if(message.Role == MessageRole.Assistant)
                    AppendModelMessage(message, functionCallingTurn);
                else
                    AppendToolMessage(message.ToolContext!);

            sb.AppendLine(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.Turn));
        }

        private void AppendModelMessage(ChatMessage message, bool functionCallingTurn)
        {
            var messageContent = message.Content!;
            if(message.ContentType == ContentType.Reasoning)
                AppendModelMessageThinking(messageContent, functionCallingTurn);
            else
                AppendModelMessageResponse(messageContent);
        }

        private void AppendModelMessageThinking(string messageContent, bool functionCallingTurn)
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.Channel))
                .AppendLine(Gemma4Keywords.Thought)
                .Append(functionCallingTurn ? messageContent : $"{Gemma4Tokens.TokenStart(Gemma4TokenKeywords.Channel)}{Gemma4Keywords.Thought}\n{Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.Channel)}")
                .Append(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.Channel));
        }

        private void AppendModelMessageResponse(string messageContent)
        {
            sb.Append(messageContent);
        }

        private void AppendToolMessage(ToolContext toolContext)
        {
            if(toolContext.Result)
                AppendToolResponse(toolContext);
            else
                AppendToolCall(toolContext);
        }

        private void AppendToolCall(ToolContext toolContext)
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.ToolCall))
                .Append(Gemma4Keywords.Call)
                .Append(':')
                .Append(toolContext.ToolName)
                .Append(toolContext.Content)
                .Append(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.ToolCall));
            
        }

        private void AppendToolResponse(ToolContext toolContext)
        {
            sb.Append(Gemma4Tokens.TokenStart(Gemma4TokenKeywords.ToolResponse))
                .Append(Gemma4Keywords.Response)
                .Append(':')
                .Append(toolContext.ToolName)
                .Append(toolContext.Content)
                .Append(Gemma4Tokens.TokenEnd(Gemma4TokenKeywords.ToolResponse));
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

            Type = ProviderOptionType.String,

            DefaultValue = "gemma4:31b"
        };

        public readonly static ProviderOptionDescriptor Thinking = new()
        {
            Name = "Thinking",
            Key = "thinking",
            Description = "Wether model should use thinking or not.",

            Type = ProviderOptionType.Boolean,

            DefaultValue = false,
        };
    }

    private static class ExtendedMessagePropertyKeys
    {
    }

    private static class Gemma4Roles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Model = "model";
    }

    private static class Gemma4Tokens
    {
        public const string Thinking = "<|think|>";
        public const string ToolQuote = "<|\"|>";
        public static string TokenStart(string a) => $"<|{a}>";
        public static string TokenEnd(string a) => $"<{a}|>";
        public static string Initiator(bool thinking) => $"\n{TokenStart(Gemma4TokenKeywords.Turn)}{Gemma4Roles.Model}\n{(
            thinking 
                ? "" 
                : $"{TokenStart(Gemma4TokenKeywords.Channel)}{Gemma4Keywords.Thought}\n{TokenEnd(Gemma4TokenKeywords.Channel)}")}";
    }

    private static class Gemma4TokenKeywords
    {
        public const string Turn = "turn";
        public const string Channel = "channel";
        public const string Tool = "tool";
        public const string ToolCall = "tool_call";
        public const string ToolResponse = "tool_response";
    }

    private static class Gemma4Keywords
    {
        public const string Thought = "thought";
        public const string Declaration = "declaration";
        public const string Call = "call";
        public const string Response = "response";
    }

    private static class Gemma4ToolKeywords
    {
        public const string Parameters = "parameters";
        public const string Properties = "properties";
        public const string Description = "description";
        public const string Type = "type";
        public const string Enum = "enum";
        public const string Required = "required";
    }
}

public static class LinqExtensions
{
    public static IEnumerable<List<T>> GroupAdjacent<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        using var iterator = source.GetEnumerator();
        if(!iterator.MoveNext()) yield break;

        var currentKey = keySelector(iterator.Current);
        var currentGroup = new List<T> { iterator.Current };

        while(iterator.MoveNext())
        {
            var nextKey = keySelector(iterator.Current);
            if(EqualityComparer<TKey>.Default.Equals(currentKey, nextKey))
                currentGroup.Add(iterator.Current);
            else
            {
                yield return currentGroup;
                currentKey = nextKey;
                currentGroup = [iterator.Current];
            }
        }
        yield return currentGroup;
    }
}
