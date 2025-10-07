namespace ChatBackend.Models;

public class ChatHistory
{
    public List<ChatMessage> Messages { get; set; } = [];

    public DateTime? LastMessageTime => Messages.LastOrDefault()?.Timestamp;

    private string? _title;
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