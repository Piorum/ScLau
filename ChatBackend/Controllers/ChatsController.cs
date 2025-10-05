using Microsoft.AspNetCore.Mvc;
using ChatBackend.Models;
using System.Text.Json;
using System.Text;
using System.Collections.Concurrent;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    //Temp
    private static readonly ConcurrentDictionary<string, ChatHistory> _chats = [];
    private static readonly GptOss _chatGenerator = new();

    // GET /api/chats
    [HttpGet]
    public IActionResult GetChats()
    {
        var chatsData = _chats.Select(kvp => new
        {
            ChatId = kvp.Key,
            lastMessage = new DateTimeOffset(kvp.Value.LastMessageTime ?? DateTime.UnixEpoch).ToUnixTimeSeconds()
        });

        return Ok(chatsData);
    }

    // GET /api/chats/{chatId}
    [HttpGet("{chatId}")]
    public async Task GetChat(string chatId)
    {
        Response.ContentType = "application/x-ndjson";

        _chats.TryGetValue(chatId, out var history);
        if (history is null)
        {
            history = new();
            _chats.TryAdd(chatId, history);
        }

        foreach (var message in history.Messages)
        {
            var lsrMessage = message with { Content = LatexStreamRewriter.ProcessString(message.Content) };
            var jsonChunk = JsonSerializer.Serialize(lsrMessage);
            await Response.WriteAsync(jsonChunk + "\n");
            await Response.Body.FlushAsync();
        }
    }


    // POST /api/chats/{chatId}/messages
    [HttpPost("{chatId}/messages")]
    public async Task PostMessage(string chatId, [FromBody] PostMessageRequest request)
    {

        _chats.TryGetValue(chatId, out var history);
        if (history is null)
        {
            history = new();
            _chats.TryAdd(chatId, history);
        }

        if (!string.IsNullOrEmpty(request.UserPrompt))
                history.Messages.Add(new ChatMessage
                {
                    MessageId = request.UserMessageId,
                    Role = MessageRole.User,
                    Content = request.UserPrompt
                });

        var options = request.Options ?? new ChatOptions();
        options.ModelName = "gpt-oss:20b";
        options.SystemMessage = "You are a large language model (LLM).";
        options.ExtendedProperties.TryAdd("reasoning_level", "medium");
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

        public static string ProcessString(string input)
        {
            var lsr = new LatexStreamRewriter();
            return lsr.ProcessChunk(input);
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