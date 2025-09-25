using System.Threading.Channels;
using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IChatGeneration
{
    ChannelReader<ModelResponse> ContinueChatAsync(
        ChatHistory history,
        ChatOptions options
    );
}