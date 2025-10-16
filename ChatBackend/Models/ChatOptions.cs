using ChatBackend.Interfaces;
using ChatBackend.Models.Ollama;

namespace ChatBackend.Models;

public class ChatOptions : IExtensibleProperties
{
    public string ChatProvider { get; set; } = "";
    public string SystemMessage { get; set; } = "";
    public OllamaOptions? ModelOptions { get; set; } = null;
    public List<string>? EnabledTools { get; set; } = null;
    public IDictionary<string, object> ExtendedProperties { get; private set; } = new Dictionary<string, object>();
}