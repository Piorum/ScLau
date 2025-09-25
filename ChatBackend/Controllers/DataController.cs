using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    public record PromptRequest(string UserPrompt, ulong UserMessageId);

    [HttpPost]
    public async Task PostMessageAsync([FromBody] PromptRequest request)
    {
        Response.ContentType = "application/json";
        var channelReader = GptOss.ContinueChat(0, messageId: request.UserMessageId, userPrompt: request.UserPrompt);

        var rewriter = new LatexStreamRewriter();

        await foreach (var gptOssResponse in channelReader.ReadAllAsync())
        {
            if(gptOssResponse.Response is not null)
                gptOssResponse.Response = rewriter.ProcessChunk(gptOssResponse.Response);

            var jsonChunk = System.Text.Json.JsonSerializer.Serialize(gptOssResponse);

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
