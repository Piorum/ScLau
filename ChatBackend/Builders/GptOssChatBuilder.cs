using System.Text;
using ChatBackend.Models.GptOss;

namespace ChatBackend.Builders;

public class GptOssChatBuilder
{
    private GptOssPromptBuilder? gopb = null;
    private readonly List<GptOssMessage> messages = [];

    private bool compiled = false;
    private readonly StringBuilder sb = new();

    private bool withAssistantTrail = false;

    public GptOssChatBuilder Append(GptOssChatChunk chatChunk, ulong messageId)
    {
        messages.Add(new GptOssMessage(
            roleText: $"{chatChunk.Role.ToString().ToLower()}",
            channelText: chatChunk.Channel != GptOssChannel.None ? $"{chatChunk.Channel.ToString().ToLower()}" : null,
            message: $"{chatChunk.Message}",
            stopToken: GptOssStopToken.End,
            messageId: messageId
        ));

        compiled = false;
        return this;
    }

    public GptOssChatBuilder AppendToolCall(string functionName, string message, ulong messageId)
    {
        messages.Add(new GptOssMessage(
            roleText: $"assistant",
            channelText: $"commentary to=functions.{functionName}<|constrain|>json",
            message: $"{message}",
            stopToken: GptOssStopToken.Call,
            messageId: messageId
        ));

        compiled = false;
        return this;
    }

    public GptOssChatBuilder AppendMalformedToolCall(string channelRaw, string message, ulong messageId)
    {
        messages.Add(new GptOssMessage(
            roleText: $"assistant",
            channelText: $"{channelRaw}",
            message: $"{message}",
            stopToken: GptOssStopToken.Call,
            messageId: messageId
        ));

        compiled = false;
        return this;
    }

    public GptOssChatBuilder AppendToolResult(string functionName, GptOss.IToolCallResult result, ulong messageId)
    {
        messages.Add(new GptOssMessage(
            roleText: $"functions.{functionName} to=assistant",
            channelText: $"commentary",
            message: $"{result.Seralize()}",
            stopToken: GptOssStopToken.End,
            messageId: messageId
        ));

        compiled = false;
        return this;
    }

    public GptOssChatBuilder TrailingPrompt(GptOssPromptBuilder gptOssPromptBuilder)
    {
        gopb = gptOssPromptBuilder;

        compiled = false;
        return this;
    }

    public GptOssChatBuilder WithAssistantTrail()
    {
        withAssistantTrail = true;
        return this;
    }

    public override string ToString()
    {
        if (!compiled)
            Compile();

        return $"{sb}";
    }

    public void Clear()
    {
        compiled = false;
        messages.Clear();
    }

    public void Remove(List<ulong> messageIds)
    {
        messages.RemoveAll(m => messageIds.Contains(m.MessageId));
    }

    private void Compile()
    {
        sb.Clear();
        if (gopb is not null)
            sb.Append($"{gopb}");

        foreach (var message in messages)
        {
            sb.Append($"<|start|>{message.RoleText}");

            if (message.ChannelText != null)
                sb.Append($"<|channel|>{message.ChannelText}");

            sb.Append($"<|message|>{message.Message}");

            var stopTokenString = message.StopToken switch
            {
                GptOssStopToken.Return => "<|return|>",
                GptOssStopToken.Call => "<|call|>",
                _ => "<|end|>"
            };

            sb.Append(stopTokenString);
        }

        if (withAssistantTrail)
            sb.Append("<|start|>assistant");

        compiled = true;
    }

    private readonly record struct GptOssMessage
    {
        public readonly string RoleText;
        public readonly string? ChannelText = null;
        public readonly string Message;
        public readonly GptOssStopToken StopToken;
        public readonly ulong MessageId;

        public GptOssMessage(string roleText, string? channelText, string message, GptOssStopToken stopToken, ulong messageId)
        {

            RoleText = roleText;
            ChannelText = channelText;
            Message = message;
            StopToken = stopToken;
            MessageId = messageId;
        }
    };

    private enum GptOssStopToken
    {
        End,
        Return,
        Call
    }

}

public record GptOssChatChunk(GptOssRole Role, GptOssChannel Channel, string Message);
