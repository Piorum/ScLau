namespace ChatBackend.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ToolParameterAttribute(string name, string description, bool isRequired) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public bool IsRequired { get; } = isRequired;
}
