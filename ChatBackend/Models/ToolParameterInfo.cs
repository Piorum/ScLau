namespace ChatBackend.Models;

public class ToolParameterInfo
{
    required public string Name;
    required public string Description;
    required public Type Type;
    required public bool IsRequired;
    public object? DefaultValue;
    public IEnumerable<string>? EnumValues;
}
