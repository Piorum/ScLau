using System.Text.Json;
using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IToolExecutor
{
    Task<ToolResult> ExecuteAsync(string toolName, JsonElement parameters);
}
