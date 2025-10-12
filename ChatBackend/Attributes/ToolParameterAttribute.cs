namespace ChatBackend.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ToolParameterAttribute(string name, string description, bool isRequired = true, object? defaultValue = null) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public bool IsRequired { get; } = isRequired;
    public object? DefaultValue { get; } = defaultValue;
}
