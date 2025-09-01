using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
            var channelReader = Ollama.GetCompletion(userPrompt);

            await foreach (var ollamaResponse in channelReader.ReadAllAsync())
            {
                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(new { ollamaResponse.Response });
                await Response.WriteAsync(jsonChunk);
                await Response.Body.FlushAsync();
            }
        }
    }
}