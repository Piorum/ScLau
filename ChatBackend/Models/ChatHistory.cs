namespace ChatBackend.Models;

public class ChatHistory
{
    public Guid Id { get; set; } //PK
    public List<ChatMessage> Messages { get; set; } = [];
    public ChatOptions? LastChatOptions { get; set; } = null;
    private string? _title;

    public DateTime? LastMessageTime => Messages.LastOrDefault()?.Timestamp;

    public string Title
    {
        get
        {
            if (!string.IsNullOrEmpty(_title))
                return _title;

            var time = LastMessageTime ?? DateTime.Now;
            return $"Chat {time:G}";
        }
        set => _title = value;
    }

}