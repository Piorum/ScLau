namespace ChatBackend.Models;

public class ChatOptions
{
    public string SystemMessage { get; set; } = "";
    public string ModelName { get; set; } = "";
    public double Temperature { get; set; } = 1.0;
    public Dictionary<string, object> ExtendedOptions { get; set; } = [];
}