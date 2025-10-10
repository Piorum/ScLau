namespace ChatBackend.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}
