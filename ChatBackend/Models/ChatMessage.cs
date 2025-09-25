namespace ChatBackend.Models;

public enum MessageRole { User, Assistant, Tool }

public class ChatMessage
{
    public Guid MessageId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ExtendedProperties { get; set; } = [];
}