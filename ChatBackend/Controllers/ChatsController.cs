using Microsoft.AspNetCore.Mvc;
using ChatBackend.Models;
using System.Text.Json;
using System.Text;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly GptOss _chatGenerator = new();

    // GET /api/chats
    [HttpGet]
    public IActionResult GetChats()
    {
        // Fake data: return a list of fake chats
        return Ok(new[]
        {
            new { ChatId = "chat1", Title = "My First Chat", LastMessage = "2025-09-25T10:00:00Z" },
            new { ChatId = "chat2", Title = "Architecture Discussion", LastMessage = "2025-09-25T12:30:00Z" }
        });
    }

    // GET /api/chats/{chatId}
    [HttpGet("{chatId}")]
    public IActionResult GetChat(string chatId)
    {
        // Fake data: return a fake chat history
        var history = new ChatHistory();
        history.Messages.Add(new ChatMessage { Role = MessageRole.User, Content = "Hello, world!" });
        history.Messages.Add(new ChatMessage { Role = MessageRole.Assistant, Content = "Hi! I'm a fake assistant." });
        return Ok(history);
    }

    // POST /api/chats/{chatId}/messages
    [HttpPost("{chatId}/messages")]
    public async Task PostMessage(string chatId, [FromBody] PostMessageRequest request)
    {
        // For now, we'll create a dummy history.
        // Later, this will come from a database based on chatId.
        var history = new ChatHistory();
        history.Messages.Add(new ChatMessage
        {
            MessageId = request.UserMessageId,
            Role = MessageRole.User,
            Content = request.UserPrompt
        });

        var options = request.Options ?? new ChatOptions();
        options.ModelName = "gpt-oss:20b";
        options.SystemMessage = "You are a large language model (LLM).";
        options.ExtendedProperties.TryAdd("developer_message", "Fulfill the request to the best of your abilities.");

        LatexStreamRewriter lsr = new();
        Response.ContentType = "application/x-ndjson";
        await foreach (var response in _chatGenerator.ContinueChatAsync(history, options).ReadAllAsync())
        {
            response.ContentChunk = lsr.ProcessChunk(response.ContentChunk);
            var jsonChunk = JsonSerializer.Serialize(response);
            await Response.WriteAsync(jsonChunk + "\n");
            await Response.Body.FlushAsync();
        }
    }

    private class LatexStreamRewriter
    {
        private bool waitingForNext = false;
        private readonly StringBuilder sb = new();

        public string ProcessChunk(string token)
        {
            sb.Clear();

            foreach (var c in token)
                if (waitingForNext)
                {
                    switch (c)
                    {
                        case '(':
                        case ')':
                            sb.Append('$');
                            break;
                        case '[':
                        case ']':
                            sb.Append("$$");
                            break;
                        default:
                            sb.Append('\\').Append(c); // not math, output the backslash + current char
                            break;
                    }

                    waitingForNext = false;
                }
                else
                    if (c == '\\')
                    waitingForNext = true; // hold until next character
                else
                    sb.Append(c);

            return $"{sb}";
        }
    }
}

// DTO for the request body
public class PostMessageRequest
{
    public string UserPrompt { get; set; } = "";
    public Guid UserMessageId { get; set; }
    public ChatOptions? Options { get; set; }
}