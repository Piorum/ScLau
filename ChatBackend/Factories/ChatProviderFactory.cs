using ChatBackend.Interfaces;

namespace ChatBackend.Factories;

public class ChatProviderFactory(IEnumerable<IChatProvider> providers) : IChatProviderFactory
{
    private readonly Dictionary<string, IChatProvider> _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    public IChatProvider GetProvider(string name)
    {
        if (_providers.TryGetValue(name, out var provider))
        {
            return provider;
        }
        throw new NotSupportedException($"Provider '{name}' is not supported.");
    }
}