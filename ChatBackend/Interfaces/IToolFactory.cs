using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IToolFactory
{
    IEnumerable<string> ToolNames();
    ToolInfo GetToolInfo(string name);
    ITool GetTool(string name);
}
