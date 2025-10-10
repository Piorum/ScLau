using ChatBackend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProviderssController(IChatProviderFactory chatProviderFactory, IEnumerable<IChatProvider> chatProviders) : ControllerBase
{
    private readonly IChatProviderFactory _chatProviderFactory = chatProviderFactory;
    private readonly IEnumerable<IChatProvider> _chatProviders = chatProviders;

    // GET /api/providers
    // Returns a list of the available providers by name    
    [HttpGet]
    public IActionResult GetProviders()
    {
        return Ok(_chatProviders.Select(p => p.Name));
    }

    // GET /api/providers/{providerName}/options
    // Returns a list of provider specific options, their descriptions, and valid values if applicable
    [HttpGet("{providerName}/options")]
    public IActionResult GetChat(string providerName)
    {
        try
        {
            var provider = _chatProviderFactory.GetProvider(providerName);
            return Ok(provider.ExtendedOptions);
        }
        catch (NotSupportedException ex)
        {
            return NotFound(ex.Message);

        }
    }

    // GET /api/providers/{providerName}/tools
    // Returns a list of tools available to the provider, their names and descriptions
}
