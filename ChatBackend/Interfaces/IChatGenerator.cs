using System.Threading.Channels;
using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IChatGenerator
{
    ChannelReader<ModelResponse> ContinueChatAsync(
        ChatHistory history,
        ChatOptions options
    );
}