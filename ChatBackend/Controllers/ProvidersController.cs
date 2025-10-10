using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProviderssController : ControllerBase
{
    // GET /api/providers
    // Returns a list of the available providers by name

    // GET /api/providers/{providerName}/options
    // Returns a list of provider specific options, their descriptions, and valid values if applicable
    
    // GET /api/providers/{providerName}/tools
    // Returns a list of tools available to the provider, their names and descriptions
}
