using System.Threading.Channels;
using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IChatGenerator
{
    string Name { get; }
    IEnumerable<ProviderOptionDescriptor> ExtendedOptions { get; }

    ChannelReader<ModelResponse> ContinueChatAsync(
        ChatHistory history,
        ChatOptions options
    );

}