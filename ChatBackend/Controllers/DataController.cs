using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DataController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMessageAsync()
    {
        var aiResponse = await OllamaTest.GetCompletion();
        var message = new { Text = aiResponse };
        return Ok(message);
    }
}

