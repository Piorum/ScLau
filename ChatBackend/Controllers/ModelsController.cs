using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    // GET /api/models
    // Returns a list of the available models by name and their description

    // GET /api/models/{modelName}
    // Returns a list of model specific parameters, available tool names and their descriptions
}
