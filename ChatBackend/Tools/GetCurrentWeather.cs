using System.Text.Json.Serialization;
using ChatBackend.Attributes;
using ChatBackend.Interfaces;

namespace ChatBackend.Tools;

public class WeatherParameters
{
    [ToolParameter("location", "The city and state, e.g. San Francisco, CA", true)]
    public string Location { get; set; } = "";

    [ToolParameter("format", "default: celsius", false)]
    public WeatherFormats Format { get; set; } = WeatherFormats.Celsius;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WeatherFormats
{
    Celsius,
    Fahrenheit
}

[Tool("get_current_weather", "Gets the current weather in the provided location.")]
public class GetCurrentWeather : ITool<WeatherParameters>
{
    public Task<string?> InvokeAsync(WeatherParameters paramObject)
    {
        string result = "{\"sunny\": true, \"temperature\": 20}";

        return Task.FromResult<string?>(result);
    }
}
