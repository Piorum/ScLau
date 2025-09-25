namespace ChatBackend.Models;

public class ChatOptions
{
    public string ModelName { get; set; } = "gpt-oss:20b";
    public double Temperature { get; set; } = 1.0;
    public Dictionary<string, object> ExtendedOptions { get; set; } = [];
}