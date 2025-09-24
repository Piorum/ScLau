using System.Text;
using ChatBackend.Models.GptOss;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    [HttpPost]
    public async Task PostMessageAsync([FromBody] string userPrompt)
    {
        Response.ContentType = "application/json";
        var channelReader = GptOss.ContinueChat(0, userPrompt);

        var rewriter = new LatexStreamRewriter();

        Random rand = new((int)DateTimeOffset.Now.ToUnixTimeSeconds());
        string currentChannel = GptOssChannel.Analysis.ToString().ToLower();

        ulong currentId = (ulong)rand.NextInt64();

        await foreach (var gptOssResponse in channelReader.ReadAllAsync())
        {
            if (gptOssResponse.Channel is not null && !gptOssResponse.Channel.Equals(currentChannel, StringComparison.CurrentCultureIgnoreCase))
            {
                currentChannel = gptOssResponse.Channel.ToLower();
                currentId = (ulong)rand.NextInt64();
            }
            gptOssResponse.MessageId = currentId;

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
