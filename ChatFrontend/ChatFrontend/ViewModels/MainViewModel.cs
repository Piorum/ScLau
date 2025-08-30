using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatFrontend.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _backendMessage;

    public readonly HttpClient _httpClient = new() { BaseAddress = new Uri("http://localhost:8080") };

    [RelayCommand]
    private async Task FetchMessageFromBackend()
    {
        try
        {
            var url = "/api/data";

            BackendMessage = "Fetching...";

            var response = await _httpClient.GetFromJsonAsync(url, AppJsonSerializerContext.Default.MessageDto);

            if (response is not null)
            {
                BackendMessage = response.Text;
            }
            else
            {
                BackendMessage = "Failed to get a valid response.";
            }
        }
        catch (HttpRequestException ex)
        {
            BackendMessage = $"HTTP Error: {ex.StatusCode} - {ex.Message}";
            await Console.Out.WriteLineAsync($"HttpRequestException: {ex}");
        }
        catch (Exception ex)
        {
            // This is a catch-all for any other errors (e.g., JSON parsing issues)
            BackendMessage = $"An unexpected error occurred: {ex.Message}";
            await Console.Out.WriteLineAsync($"Generic Exception: {ex}");
        }
    }
}

public class MessageDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MessageDto))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
