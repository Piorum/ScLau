using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    [HttpPost]
    public async Task PostMessageAsync([FromBody] string userPrompt)
    {
        Response.ContentType = "text/plain";
        await foreach (var chunk in OllamaTest.GetCompletion(userPrompt))
        {
            await Response.WriteAsync(chunk);
            await Response.Body.FlushAsync();
        }
    }
}

