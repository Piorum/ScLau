using ChatBackend.Interfaces;

namespace ChatBackend.Models;

public enum MessageRole { User, Assistant, Tool }

public record ChatMessage : IExtensibleProperties
{
    public Guid MessageId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = "";
    public ContentType ContentType { get; set; } = ContentType.Answer;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public IDictionary<string, object> ExtendedProperties { get; private set; } = new Dictionary<string, object>();
}