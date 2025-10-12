using System.Text.Json;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend.Tools;

public class ToolExecutor : IToolExecutor
{
    public Task<ToolResult> ExecuteAsync(string toolName, JsonElement parameters)
    {
        throw new NotImplementedException();
    }
}
