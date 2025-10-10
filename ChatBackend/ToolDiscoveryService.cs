using System.Reflection;
using ChatBackend.Attributes;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend;

public class ToolDiscoveryService : IToolDiscoveryService
{
    private readonly Lazy<List<ToolInfo>> _lazyTools;

    public ToolDiscoveryService()
    {
        _lazyTools = new Lazy<List<ToolInfo>>(GetToolsInternal);
    }

    public IEnumerable<ToolInfo> GetTools() => _lazyTools.Value;

    private static List<ToolInfo> GetToolsInternal()
    {
        var toolInfos = new List<ToolInfo>();
        var toolInterfaceType = typeof(ITool<>);

        // Find all concrete classes in the current assembly that implement ITool<T>
        var toolImplementations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == toolInterfaceType));

        foreach (var toolType in toolImplementations)
        {
            var toolAttribute = toolType.GetCustomAttribute<ToolAttribute>();
            if (toolAttribute == null) continue;

            var genericArgument = toolType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == toolInterfaceType)
                .GetGenericArguments()[0];

            object? parameterObjectInstance = null;
            try
            {
                parameterObjectInstance = Activator.CreateInstance(genericArgument);
            }
            catch
            {
                // This class has no parameterless constructor, so we can't get default values.
                // This is acceptable; default values are an optional enhancement.
            }

            var parameters = new List<ToolParameterInfo>();
            foreach (var property in genericArgument.GetProperties())
            {
                var paramAttribute = property.GetCustomAttribute<ToolParameterAttribute>();
                if (paramAttribute == null) continue;

                var paramInfo = new ToolParameterInfo
                {
                    Name = property.Name,
                    Type = property.PropertyType,
                    Description = paramAttribute.Description,
                    IsRequired = paramAttribute.IsRequired,
                    DefaultValue = parameterObjectInstance != null ? property.GetValue(parameterObjectInstance) : null,
                    EnumValues = GetEnumValues(property.PropertyType)
                };
                parameters.Add(paramInfo);
            }

            toolInfos.Add(new ToolInfo
            {
                Name = toolAttribute.Name,
                Description = toolAttribute.Description,
                ParametersType = genericArgument,
                ToolType = toolType,
                Parameters = parameters
            });
        }

        return toolInfos;
    }

    private static IEnumerable<string>? GetEnumValues(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (underlyingType.IsEnum)
        {
            // Using ToLower() to match the JSON/TypeScript format
            return Enum.GetNames(underlyingType).Select(e => e.ToLowerInvariant());
        }
        return null;
    }

}
