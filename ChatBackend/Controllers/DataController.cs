using ChatBackend.Models.GptOss;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        [HttpPost]
        public async Task PostMessageAsync([FromBody] string userPrompt)
        {
            Response.ContentType = "application/json";
            var channelReader = GptOss.ContinueChat(0, userPrompt);

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

                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(gptOssResponse);
                await Response.WriteAsync(jsonChunk + "\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}