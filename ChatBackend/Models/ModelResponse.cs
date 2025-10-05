namespace ChatBackend.Models;

public class ModelResponse
{
    public Guid MessageId { get; set; }
    public ContentType ContentType { get; set; }
    public string ContentChunk { get; set; } = "";
    public bool IsDone { get; set; } = false;
}