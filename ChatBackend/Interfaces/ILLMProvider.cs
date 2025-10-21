using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface ILLMProvider
{
    IAsyncEnumerable<string> StreamCompletionAsync(string prompt, string modelName, ChatOptions options, CancellationToken cancellationToken = default);
}
