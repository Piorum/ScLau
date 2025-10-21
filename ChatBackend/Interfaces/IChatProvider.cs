using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IChatProvider
{
    string Name { get; }
    IEnumerable<ProviderOptionDescriptor> ExtendedOptions { get; }

    //This shouldn't throw, handle cancellation and complete gracefully.
    IAsyncEnumerable<ModelResponse> ContinueChatAsync(
        ChatHistory history,
        ChatOptions options,
        CancellationToken cancellationToken = default
    );

}