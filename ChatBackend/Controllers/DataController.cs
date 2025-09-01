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
        await foreach (var chunk in Ollama.GetCompletion(userPrompt))
        {
            var jsonChunk = System.Text.Json.JsonSerializer.Serialize(new { chunk });
            await Response.WriteAsync(jsonChunk);
            await Response.Body.FlushAsync();
        }
    }
}

