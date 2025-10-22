using System.Text.Json.Serialization;
using ChatBackend.Interfaces;

namespace ChatBackend.Models;

public record ChatMessage : IExtensibleProperties
{
    public Guid MessageId { get; init; } //Key
    public MessageRole Role { get; init; }
    public string? Content { get; set; } = null;
    public ToolContext? ToolContext { get; set; } = null;
    public ContentType ContentType { get; init; } = ContentType.Answer;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public IDictionary<string, object> ExtendedProperties { get; init; } = new Dictionary<string, object>();

    [JsonConstructor]
    private ChatMessage() { }

    private ChatMessage(Guid messageId, MessageRole role, string content, ContentType? contentType = null)
    {
        MessageId = messageId;
        Role = role;
        Content = content;

        if (contentType.HasValue)
            ContentType = contentType.Value;
    }

    public static ChatMessage CreateUserMessage(Guid messageId, string content, ContentType? contentType = null) =>
        new(messageId, MessageRole.User, content, contentType);
    public static ChatMessage CreateAssistantMessage(Guid messageId, string content, ContentType? contentType = null) =>
        new(messageId, MessageRole.Assistant, content, contentType);
    public static ChatMessage CreateSystemMessage(Guid messageId, string content, ContentType? contentType = null) =>
        new(messageId, MessageRole.System, content, contentType);

    public static ChatMessage CreateToolMessage(Guid messageId, ToolContext toolContext) =>
        new()
        {
            MessageId = messageId,
            Role = MessageRole.Tool,
            ToolContext = toolContext,
            ContentType = ContentType.Reasoning
        };

}

public record ToolContext(string Id, string ToolName, string Content, bool Result);