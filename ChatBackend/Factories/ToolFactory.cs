using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend.Factories;

public class ToolFactory : IToolFactory
{
    public ITool GetTool(string name)
    {
        throw new NotImplementedException();
    }

    public ToolInfo GetToolInfo(string name)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> ToolNames()
    {
        throw new NotImplementedException();
    }
}
