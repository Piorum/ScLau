using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DataController : ControllerBase
{
    [HttpGet]
    [HttpGet]
    public async Task GetMessageAsync()
    {
        Response.ContentType = "text/plain";
        await foreach (var chunk in OllamaTest.GetCompletion())
        {
            Console.WriteLine($"Sending chunk: {chunk}");
            await Response.WriteAsync(chunk);
            await Response.Body.FlushAsync();
        }
    }
}

