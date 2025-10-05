namespace ChatBackend.Models;

public class ChatHistory
{
    public List<ChatMessage> Messages { get; set; } = [];

    public DateTime? LastMessageTime => Messages.LastOrDefault()?.Timestamp;
}