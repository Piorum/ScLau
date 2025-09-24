using System.Text;
using ChatBackend.Models.GptOss;

namespace ChatBackend.Builders;

public class GptOssChatBuilder
{
    private GptOssPromptBuilder? gopb = null;
    private readonly StringBuilder sb = new();

    public GptOssChatBuilder Append(GptOssChatChunk chatChunk)
    {
        sb.Append($"<|start|>{chatChunk.Role.ToString().ToLower()}");
        if (chatChunk.Channel != GptOssChannel.None)
            sb.Append($"<|channel|>{chatChunk.Channel.ToString().ToLower()}");

        sb.Append($"<|message|>{chatChunk.Message}<|end|>");

        return this;
    }

    public GptOssChatBuilder AppendToolCall(string functionName, string message)
    {
        sb.Append($"<|start|>assistant");
        sb.Append($"<|channel|>commentary to=functions.{functionName}<|constrain|>json");
        sb.Append($"<|message|>{message}");
        sb.Append($"<|call|>");

        return this;
    }
    
    public GptOssChatBuilder AppendMalformedToolCall(string channelRaw, string message)
    {
        sb.Append($"<|start|>assistant");
        sb.Append($"<|channel|>{channelRaw}");
        sb.Append($"<|message|>{message}");
        sb.Append($"<|call|>");

        return this;
    }

    public GptOssChatBuilder AppendToolResult(string functionName, GptOss.IToolCallResult result)
    {
        sb.Append($"<|start|>functions.{functionName} to=assistant");
        sb.Append($"<|channel|>commentary");
        sb.Append($"<|message|>{result.Seralize()}");
        sb.Append($"<|end|>");

        return this;
    }

    public GptOssChatBuilder WithPrompt(GptOssPromptBuilder gptOssPromptBuilder)
    {
        gopb = gptOssPromptBuilder;
        return this;
    }

    public string WithAssistantTrail()
    {
        return $"{this}<|start|>assistant";
    }

    public override string ToString()
    {
        if (gopb is not null)
            return $"{gopb}{sb}";
        else
            return $"{sb}";
    }

    public void Clear()
    {
        sb.Clear();
    }

}

public record GptOssChatChunk(GptOssRole Role, GptOssChannel Channel, string Message);
