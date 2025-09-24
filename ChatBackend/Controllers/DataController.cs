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

            await foreach (var gptOssResponse in channelReader.ReadAllAsync())
            {
                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(gptOssResponse);
                await Response.WriteAsync(jsonChunk + "\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}