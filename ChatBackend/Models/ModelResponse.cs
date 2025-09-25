namespace ChatBackend.Models;

public class ModelResponse
{
    public Guid MessageId { get; set; }
    public ContentType ContentType { get; set; } // "reasoning", "answer", "error"
    public string ContentChunk { get; set; } = "";
    public bool IsDone { get; set; } = false;
}

public enum ContentType
{
    Reasoning,
    Answer
}