using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatBackend.Attributes;
using ChatBackend.Interfaces;

namespace ChatBackend.Tools;

//Claude Sonnet 4.6

// ══════════════════════════════════════════════════════════════════
//  Static shared Firecrawl client
//  Configure once at startup before any tools are invoked:
//
//    FirecrawlService.Configure("http://192.168.1.50:3002");
//    // or with an API key:
//    FirecrawlService.Configure("http://api:3002", apiKey: "fc-...");
//
//  Call this before (or at the top of) your own ModuleInitializer,
//  or at the start of Program.cs / Main().
// ══════════════════════════════════════════════════════════════════
 
public static class FirecrawlService
{
    // HttpClient is safe and recommended as a long-lived static instance.
    private static HttpClient?      _http;
    private static FirecrawlClient? _client;
 
    public static FirecrawlClient Client =>
        _client ?? throw new InvalidOperationException(
            "FirecrawlService has not been configured. " +
            "Call FirecrawlService.Configure(baseUrl) before using any Firecrawl tools.");
 
    /// <summary>
    /// Call once at startup — e.g. in Program.cs or your own ModuleInitializer.
    /// </summary>
    /// <param name="baseUrl">
    ///   Where Firecrawl is running, e.g.
    ///   "http://192.168.1.50:3002"  (LAN machine)
    ///   "http://localhost:3002"     (local dev, no Docker)
    ///   "http://api:3002"           (same Docker Compose network)
    /// </param>
    /// <param name="apiKey">
    ///   Bearer token sent with every request.
    ///   Only required when USE_DB_AUTHENTICATION=true on the Firecrawl side.
    /// </param>
    public static void Configure(string baseUrl, string? apiKey = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
 
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
 
        _client = new FirecrawlClient(_http);
    }

    
    [ModuleInitializer]
    internal static void Initialize()
    {
        Configure(
            baseUrl: "http://firecrawl:3002",
            apiKey:  null
        );
    }
}
 
 
// ══════════════════════════════════════════════════════════════════
//  Shared file-scoped helpers
// ══════════════════════════════════════════════════════════════════
 
file static class ToolJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented          = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
    };
 
    /// <summary>
    /// Splits a nullable comma-separated string into a trimmed list,
    /// returning null if blank so optional params stay absent from requests.
    /// </summary>
    internal static IReadOnlyList<string>? SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
 
 
// ══════════════════════════════════════════════════════════════════
//  map_site
// ══════════════════════════════════════════════════════════════════
 
public class MapSiteParameters
{
    [ToolParameter("url",
        "The root URL to map, e.g. https://example.com",
        isRequired: true)]
    public string Url { get; set; } = "";
 
    [ToolParameter("search",
        "Optional keyword to filter discovered links. " +
        "Only URLs whose path or content relates to this term are returned.",
        isRequired: false)]
    public string? Search { get; set; }
 
    [ToolParameter("limit",
        "Maximum number of URLs to return (default 100, max 5000).",
        isRequired: false, defaultValue: 100)]
    public int Limit { get; set; } = 100;
 
    [ToolParameter("include_subdomains",
        "When true, links on sub-domains of the target host are also included.",
        isRequired: false, defaultValue: false)]
    public bool IncludeSubdomains { get; set; } = false;
 
    [ToolParameter("exclude_paths",
        "Comma-separated glob patterns for paths to exclude, " +
        "e.g. **/admin/**,**/login/**",
        isRequired: false)]
    public string? ExcludePaths { get; set; }
}
 
[Tool("map_site",
    "Discovers all publicly reachable URLs on a website without fetching page content. " +
    "Use this first to find which pages are relevant, then call scrape_page on the " +
    "URLs that look most useful. Do not stop after mapping — the links returned are " +
    "only useful if you scrape the ones relevant to the user's request.")]
public class MapSiteTool : ITool<MapSiteParameters>
{
    public async Task<string?> InvokeAsync(MapSiteParameters p)
    {
        Console.WriteLine($"[map_site] Parameters received: {JsonSerializer.Serialize(p, ToolJson.Options)}");
 
        var request = new MapRequest
        {
            Url               = p.Url,
            Search            = string.IsNullOrWhiteSpace(p.Search) ? null : p.Search,
            Limit             = p.Limit,
            IncludeSubdomains = p.IncludeSubdomains,
            ExcludePaths      = ToolJson.SplitCsv(p.ExcludePaths),
        };
 
        Console.WriteLine($"[map_site] Sending to Firecrawl: {JsonSerializer.Serialize(request, ToolJson.Options)}");
 
        try
        {
            var response = await FirecrawlService.Client.MapAsync(request);
            Console.WriteLine($"[map_site] Success — {response.Links.Count} links returned");
 
            return JsonSerializer.Serialize(
                new { success = true, count = response.Links.Count, links = response.Links },
                ToolJson.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[map_site] Exception: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
 
 
// ══════════════════════════════════════════════════════════════════
//  scrape_page
// ══════════════════════════════════════════════════════════════════
 
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScrapeOutputFormat
{
    /// <summary>Clean Markdown — best for feeding to an LLM.</summary>
    Markdown,
    /// <summary>Cleaned HTML with boilerplate stripped.</summary>
    Html,
    /// <summary>Raw HTML exactly as the browser received it.</summary>
    RawHtml,
    /// <summary>All outbound hyperlinks found on the page.</summary>
    Links,
}
 
public class ScrapePageParameters
{
    [ToolParameter("url",
        "The exact URL to scrape, e.g. https://example.com/about",
        isRequired: true)]
    public string Url { get; set; } = "";
 
    [ToolParameter("format",
        "Output format: Markdown (default, best for LLMs), Html, RawHtml, or Links.",
        isRequired: false, defaultValue: ScrapeOutputFormat.Markdown)]
    public ScrapeOutputFormat Format { get; set; } = ScrapeOutputFormat.Markdown;
 
    [ToolParameter("only_main_content",
        "When true (default), navigation bars, footers, and sidebars are stripped " +
        "so only the primary page content is returned.",
        isRequired: false, defaultValue: true)]
    public bool OnlyMainContent { get; set; } = true;
 
    [ToolParameter("wait_for_ms",
        "Extra milliseconds to wait after the page loads before scraping. " +
        "Useful for JavaScript-heavy pages that render content after load (e.g. 2000).",
        isRequired: false, defaultValue: 0)]
    public int WaitForMs { get; set; } = 0;
 
    [ToolParameter("exclude_tags",
        "Comma-separated CSS selectors to strip before converting, " +
        "e.g. nav,footer,.ads,#cookie-banner",
        isRequired: false)]
    public string? ExcludeTags { get; set; }
 
    [ToolParameter("include_tags",
        "Comma-separated CSS selectors to keep — everything else is removed. " +
        "e.g. article.content,div.main. Overrides exclude_tags when set.",
        isRequired: false)]
    public string? IncludeTags { get; set; }
 
    [ToolParameter("timeout_ms",
        "Maximum milliseconds to wait for the page to respond (default 30000).",
        isRequired: false, defaultValue: 30000)]
    public int TimeoutMs { get; set; } = 30_000;
}
 
[Tool("scrape_page",
    "Fetches a single web page and returns its content. Use Markdown format (default) " +
    "to read and summarise page content — always do this after map_site to get the " +
    "actual information the user asked for. Call this multiple times in sequence if " +
    "several pages are relevant. Once you have the content, synthesise and answer " +
    "the user directly — do not just repeat the raw scraped text.")]
public class ScrapePageTool : ITool<ScrapePageParameters>
{
    public async Task<string?> InvokeAsync(ScrapePageParameters p)
    {
        Console.WriteLine($"[scrape_page] Parameters received: {JsonSerializer.Serialize(p, ToolJson.Options)}");
 
        var formats = p.Format switch
        {
            ScrapeOutputFormat.Html    => new[] { "html" },
            ScrapeOutputFormat.RawHtml => new[] { "rawHtml" },
            ScrapeOutputFormat.Links   => new[] { "links" },
            _                          => new[] { "markdown" },
        };
 
        var request = new ScrapeRequest
        {
            Url             = p.Url,
            Formats         = formats,
            OnlyMainContent = p.OnlyMainContent,
            WaitForMs       = p.WaitForMs > 0 ? p.WaitForMs : null,
            ExcludeTags     = ToolJson.SplitCsv(p.ExcludeTags),
            IncludeTags     = ToolJson.SplitCsv(p.IncludeTags),
            TimeoutMs       = p.TimeoutMs,
        };
 
        Console.WriteLine($"[scrape_page] Sending to Firecrawl: {JsonSerializer.Serialize(request, ToolJson.Options)}");
 
        try
        {
            var response = await FirecrawlService.Client.ScrapeAsync(request);
            var data = response.Data;
            Console.WriteLine($"[scrape_page] Success — title: '{data?.Metadata?.Title}', status: {data?.Metadata?.StatusCode}");
 
            var result = new
            {
                url        = data?.Metadata?.SourceUrl ?? p.Url,
                title      = data?.Metadata?.Title,
                statusCode = data?.Metadata?.StatusCode,
                format     = p.Format.ToString(),
                content    = p.Format switch
                {
                    ScrapeOutputFormat.Html    => data?.Html,
                    ScrapeOutputFormat.RawHtml => data?.RawHtml,
                    ScrapeOutputFormat.Links   => null,
                    _                          => data?.Markdown,
                },
                links = p.Format == ScrapeOutputFormat.Links ? data?.Links : null,
            };
 
            return JsonSerializer.Serialize(result, ToolJson.Options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[scrape_page] Exception: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
// FirecrawlClient.cs
// ------------------------------------------------------------------
//  Drop this file (and FirecrawlModels.cs below) into any .NET 8/9
//  project.  Register with DI in Program.cs:
//
//    builder.Services.AddHttpClient<FirecrawlClient>(c =>
//    {
//        c.BaseAddress = new Uri(
//            builder.Configuration["Firecrawl:BaseUrl"] ?? "http://api:3002");
//        // If auth is enabled:
//        // c.DefaultRequestHeaders.Authorization =
//        //     new AuthenticationHeaderValue("Bearer",
//        //         builder.Configuration["Firecrawl:ApiKey"]);
//    });
//
//  Then inject FirecrawlClient wherever you need it.
// ------------------------------------------------------------------

// ══════════════════════════════════════════════════════════════════
//  Models
// ══════════════════════════════════════════════════════════════════

// ── /v1/map ───────────────────────────────────────────────────────

public sealed record MapRequest
{
    /// <summary>The URL whose sitemap you want to discover.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Seed URL(s) to expand the map from. Defaults to the root of <see cref="Url"/>.
    /// </summary>
    [JsonPropertyName("search")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Search { get; init; }

    /// <summary>Maximum number of links to return (default 5000).</summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Limit { get; init; }

    /// <summary>
    /// When true, include sub-domains of the target host.
    /// </summary>
    [JsonPropertyName("includeSubdomains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeSubdomains { get; init; }

    /// <summary>
    /// Glob patterns for URLs to ignore, e.g. ["**/admin/**"].
    /// </summary>
    [JsonPropertyName("excludePaths")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? ExcludePaths { get; init; }
}

public sealed record MapResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Discovered URLs.</summary>
    [JsonPropertyName("links")]
    public IReadOnlyList<string> Links { get; init; } = [];

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

// ── /v1/scrape ────────────────────────────────────────────────────

public sealed record ScrapeFormats
{
    public static readonly IReadOnlyList<string> Markdown  = ["markdown"];
    public static readonly IReadOnlyList<string> Html      = ["html"];
    public static readonly IReadOnlyList<string> RawHtml   = ["rawHtml"];
    public static readonly IReadOnlyList<string> Links     = ["links"];
    public static readonly IReadOnlyList<string> Screenshot= ["screenshot"];
    public static readonly IReadOnlyList<string> All       = ["markdown", "html", "rawHtml", "links", "screenshot"];
}

public sealed record ScrapeRequest
{
    /// <summary>The URL to scrape.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Output format(s).  Use the constants in <see cref="ScrapeFormats"/> or
    /// supply your own list, e.g. ["markdown", "links"].
    /// Defaults to ["markdown"].
    /// </summary>
    [JsonPropertyName("formats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Formats { get; init; }

    /// <summary>
    /// CSS selectors to include; everything else is stripped.
    /// Example: ["article.content", "div.main"]
    /// </summary>
    [JsonPropertyName("includeTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? IncludeTags { get; init; }

    /// <summary>
    /// CSS selectors to remove before conversion (nav, footer, ads…).
    /// </summary>
    [JsonPropertyName("excludeTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? ExcludeTags { get; init; }

    /// <summary>HTTP headers to forward to the target site.</summary>
    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>Max time in milliseconds to wait for the page (default 30 000).</summary>
    [JsonPropertyName("timeout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Additional milliseconds to wait after the page loads before scraping
    /// (useful for lazy-loaded content).
    /// </summary>
    [JsonPropertyName("waitFor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WaitForMs { get; init; }

    /// <summary>Only return the main content; strips navigation etc.</summary>
    [JsonPropertyName("onlyMainContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OnlyMainContent { get; init; }

    /// <summary>Remove all base64 images from the output.</summary>
    [JsonPropertyName("removeBase64Images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RemoveBase64Images { get; init; }

    /// <summary>
    /// Pass a JSON schema here to have Firecrawl extract structured data
    /// using an LLM (requires OPENAI_API_KEY on the server).
    /// </summary>
    [JsonPropertyName("jsonOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScrapeJsonOptions? JsonOptions { get; init; }
}

public sealed record ScrapeJsonOptions
{
    [JsonPropertyName("prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prompt { get; init; }

    /// <summary>
    /// A JSON Schema object describing the shape you want extracted.
    /// Use <see cref="JsonElement"/> so you can build it with any approach.
    /// </summary>
    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Schema { get; init; }
}

public sealed record ScrapeData
{
    [JsonPropertyName("markdown")]
    public string? Markdown { get; init; }

    [JsonPropertyName("html")]
    public string? Html { get; init; }

    [JsonPropertyName("rawHtml")]
    public string? RawHtml { get; init; }

    [JsonPropertyName("links")]
    public IReadOnlyList<string>? Links { get; init; }

    [JsonPropertyName("screenshot")]
    public string? ScreenshotBase64 { get; init; }

    /// <summary>Populated when <see cref="ScrapeRequest.JsonOptions"/> is provided.</summary>
    [JsonPropertyName("json")]
    public JsonElement? Json { get; init; }

    [JsonPropertyName("metadata")]
    public ScrapeMetadata? Metadata { get; init; }
}

public sealed record ScrapeMetadata
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("sourceURL")]
    public string? SourceUrl { get; init; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

public sealed record ScrapeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public ScrapeData? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

// ══════════════════════════════════════════════════════════════════
//  Client
// ══════════════════════════════════════════════════════════════════

/// <summary>
/// Typed HTTP client for the Firecrawl v1 API.
/// Register via <c>services.AddHttpClient&lt;FirecrawlClient&gt;(…)</c>.
/// </summary>
public sealed class FirecrawlClient(HttpClient http)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── /v1/map ───────────────────────────────────────────────────

    /// <summary>
    /// Discover all URLs on a site without fully scraping each page.
    /// Equivalent to requesting the sitemap.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP response is not successful.
    /// </exception>
    /// <exception cref="FirecrawlException">
    /// Thrown when Firecrawl returns success=false with an error message.
    /// </exception>
    public async Task<MapResponse> MapAsync(
        MapRequest request,
        CancellationToken ct = default)
    {
        using var response = await http
            .PostAsJsonAsync("/v1/map", request, _json, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<MapResponse>(_json, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from /v1/map");

        if (!result.Success)
            throw new FirecrawlException(result.Error ?? "Unknown error from /v1/map");

        return result;
    }

    // ── /v1/scrape ────────────────────────────────────────────────

    /// <summary>
    /// Scrape a single URL and return its content in the requested formats.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP response is not successful.
    /// </exception>
    /// <exception cref="FirecrawlException">
    /// Thrown when Firecrawl returns success=false with an error message.
    /// </exception>
    public async Task<ScrapeResponse> ScrapeAsync(
        ScrapeRequest request,
        CancellationToken ct = default)
    {
        using var response = await http
            .PostAsJsonAsync("/v1/scrape", request, _json, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ScrapeResponse>(_json, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty response from /v1/scrape");

        if (!result.Success)
            throw new FirecrawlException(result.Error ?? "Unknown error from /v1/scrape");

        return result;
    }
}

// ══════════════════════════════════════════════════════════════════
//  Exception
// ══════════════════════════════════════════════════════════════════

public sealed class FirecrawlException(string message) : Exception(message);