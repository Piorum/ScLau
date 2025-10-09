using ChatBackend.Interfaces;

namespace ChatBackend.Models;

public class ChatOptions : IExtensibleProperties
{
    public string SystemMessage { get; set; } = "";
    public string ChatProvider { get; set; } = "";
    public double Temperature { get; set; } = 1.0;
    public IDictionary<string, object> ExtendedProperties { get; private set; } = new Dictionary<string, object>();
}