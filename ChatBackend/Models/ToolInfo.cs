namespace ChatBackend.Models;

public class ToolInfo
{
    required public string Name;
    required public string Description;
    required public Type ToolType { get; set; }
    required public Type ParameterType { get; set; }
    required public List<ToolParameterInfo> Parameters;
}
