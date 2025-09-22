using System;
using System.Text;
using ChatBackend.Models;

namespace ChatBackend.Builders;

public class GptOssChatBuilder
{
    private GptOssPromptBuilder? gopb = null;
    private readonly StringBuilder sb = new();

    public GptOssChatBuilder Append(GptOssChatChunk chatChunk)
    {
        sb.Append($"<|start|>{chatChunk.role.ToString().ToLower()}");
        if (chatChunk.channel != GptOssChannel.None)
            sb.Append($"<|channel|>{chatChunk.channel.ToString().ToLower()}");

        sb.Append($"<|message|>{chatChunk.message}<|end|>");

        return this;
    }

    public GptOssChatBuilder AppendRaw(string text)
    {
        sb.Append(text);
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

public record GptOssChatChunk(GptOssRole role, GptOssChannel channel, string message);
