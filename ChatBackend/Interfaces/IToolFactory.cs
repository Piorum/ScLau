using System.Text.Json;
using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IToolFactory
{
    IEnumerable<string> GetToolNames();
    ToolInfo? GetToolInfo(string name);
    Task<ToolResult> ExecuteTool(string name, JsonElement parameters);
}
