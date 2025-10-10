namespace ChatBackend.Interfaces;

public interface IChatProviderFactory
{
    IChatProvider GetProvider(string name);
}
