using System.Text;
using System.Text.Json;

using var client = new HttpClient();
string url = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? throw new ("OLLAMA_ENDPOINT is null.");

//Format guide (https://cookbook.openai.com/articles/openai-harmony)

var sysPrompt = @"<|start|>system<|message|>You are ChatGPT, a large language model trained by OpenAI.
Knowledge cutoff: 2024-06
Current date: 2025-06-28

Reasoning: high

# Valid channels: analysis, commentary, final. Channel must be included for every message.
Calls to these tools must go to the commentary channel: 'functions'.<|end|>";

var developerMessage = @"<|start|>developer<|message|># Instructions

Use a friendly tone.

# Tools

## functions

namespace functions {

// Gets the location of the user.
type get_location = () => any;

// Gets the current weather in the provided location.
type get_current_weather = (_: {
// The city and state, e.g. San Francisco, CA
location: string,
format?: ""celsius"" | ""fahrenheit"", // default: celsius
}) => any;

// Gets the current weather in the provided list of locations.
type get_multiple_weathers = (_: {
// List of city and state, e.g. [""San Francisco, CA"", ""New York, NY""]
locations: string[],
format?: ""celsius"" | ""fahrenheit"", // default: celsius
}) => any;

} // namespace functions<|end|>";

//var userMessage = "<|start|>user<|message|>Can you explain how to find the volume of the shape defined by rho^2=9?<|end|><|start|>assistant";
var userMessage = "<|start|>user<|message|>Could you tell me the current weather in SF?<|end|><|start|>assistant";

var assistantReasoning = "<|channel|>analysis<|message|>Need to use function get_current_weather.<|end|>";

var assistantFunctionCall = @"<|start|>assistant<|channel|>commentary to=functions.get_current_weather <|constrain|>json<|message|>{""location"":""San Francisco""}";

var callReturn = @"<|call|><|start|>functions.get_current_weather to=assistant<|channel|>commentary<|message|>{""sunny"": true, ""temperature"": 20}<|end|><|start|>assistant";


var requestBody = new
{
    //prompt = sysPrompt + developerMessage + userMessage,
    prompt = sysPrompt + developerMessage + userMessage + assistantReasoning + assistantFunctionCall + callReturn,
    model = "gpt-oss:20b", //Install this model through Ollama
    options = new
    {
        temperature = 0.95,
        num_predict = 2000,
        num_ctx = 4096,
        //num_gpu = 0 //Configure if model is crashing (Lower = Less GPU Offload)
    },
    raw = true
};

var jsonRequest = JsonSerializer.Serialize(requestBody);
var request = new HttpRequestMessage(HttpMethod.Post, url)
{
    Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
};


try
{
    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    Console.Write("--- Streaming Start ---\n");

    int tokenCount = 0;
    using (var stream = await response.Content.ReadAsStreamAsync())
    using (var reader = new StreamReader(stream))
    {
        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonDocument.Parse(line);
            string? textChunk = chunk.RootElement.GetProperty("response").GetString();
            bool done = chunk.RootElement.GetProperty("done").GetBoolean();

            if (!string.IsNullOrEmpty(textChunk))
            {
                Console.Write(textChunk);
                tokenCount++;
            }

            if (done) break;
        }
    }

    Console.WriteLine($"\n--- Streaming End ---\nTotal Tokens : {tokenCount}");
}
catch (Exception ex)
{
    Console.WriteLine($"{ex}");
}