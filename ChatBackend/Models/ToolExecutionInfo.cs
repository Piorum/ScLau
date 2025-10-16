using ChatBackend.Interfaces;

namespace ChatBackend.Models;

public class ToolExecutionInfo
{
    required public ITool ToolInstance { get; set; }
    required public Type ParameterType { get; set; }

}
