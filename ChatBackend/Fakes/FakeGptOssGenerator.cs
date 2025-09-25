using System.Threading.Channels;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend.Fakes;

public class FakeGptOssGenerator : IChatGeneration
{
    public ChannelReader<ModelResponse> ContinueChatAsync(ChatHistory history, ChatOptions options)
    {
        var channel = Channel.CreateUnbounded<ModelResponse>();

        _ = Task.Run(async () =>
        {
            var reasoningMessageId = Guid.NewGuid();
            var answerMessageId = Guid.NewGuid();

            // Fake reasoning
            await channel.Writer.WriteAsync(new ModelResponse
            {
                MessageId = reasoningMessageId,
                ContentType = ContentType.Reasoning,
                ContentChunk = "Thinking about the user's request... "
            });
            await Task.Delay(100); // Simulate work
            await channel.Writer.WriteAsync(new ModelResponse
            {
                MessageId = reasoningMessageId,
                ContentType = ContentType.Reasoning,
                ContentChunk = "The user wants to know about GUIDs."
            });
            await Task.Delay(100);

            // Fake answer
            await channel.Writer.WriteAsync(new ModelResponse
            {
                MessageId = answerMessageId,
                ContentType = ContentType.Answer,
                ContentChunk = "This is a fake response using GUIDs. "
            });
            await Task.Delay(100);
            await channel.Writer.WriteAsync(new ModelResponse
            {
                MessageId = answerMessageId,
                ContentType = ContentType.Answer,
                ContentChunk = "The ID for this message is " + answerMessageId
            });

            // Signal completion
            await channel.Writer.WriteAsync(new ModelResponse { IsDone = true });
        });

        return channel;

    }
}