using Microsoft.AspNetCore.Mvc;
using ChatBackend.Models;
using ChatBackend.Fakes;
using System.Text.Json;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly FakeGptOssGenerator _chatGenerator = new();

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

        Response.ContentType = "application/x-ndjson";
        await foreach (var response in _chatGenerator.ContinueChatAsync(history, options).ReadAllAsync())
        {
            var jsonChunk = JsonSerializer.Serialize(response);
            await Response.WriteAsync(jsonChunk + "\n");
            await Response.Body.FlushAsync();
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