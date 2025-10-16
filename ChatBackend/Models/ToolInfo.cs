namespace ChatBackend.Models;

public class ToolInfo
{
    required public string Name;
    required public string Description;
    required public List<ToolParameterInfo> Parameters;
}
