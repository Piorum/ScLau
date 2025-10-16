using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatBackend.Attributes;
using ChatBackend.Interfaces;
using ChatBackend.Models;

namespace ChatBackend.Factories;

public class ToolFactory : IToolFactory
{
    private static readonly Dictionary<string, ToolInfo> _toolInfos = [];
    private static readonly Dictionary<string, ToolExecutionInfo> _toolExecutioninfos = [];

    [ModuleInitializer]
    internal static void Initialize()
    {
        var toolTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetCustomAttribute<ToolAttribute>() is not null &&
                        t.GetInterfaces().Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITool<>)));

        foreach(var type in toolTypes)
        {
            var attr = type.GetCustomAttribute<ToolAttribute>()!;
            var iface = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITool<>));
            var paramType = iface.GetGenericArguments()[0];
            var tool = (ITool)Activator.CreateInstance(type)!;

            var parameters = new List<ToolParameterInfo>();
            foreach (var prop in paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var pAttr = prop.GetCustomAttribute<ToolParameterAttribute>();
                if (pAttr is null) continue;

                IEnumerable<string>? enumValues =
                    prop.PropertyType.IsEnum
                        ? Enum.GetNames(prop.PropertyType)
                        : null;

                parameters.Add(new ToolParameterInfo()
                {
                    Name = pAttr.Name,
                    Description = pAttr.Description,
                    Type = prop.PropertyType,
                    IsRequired = pAttr.IsRequired,
                    DefaultValue = pAttr.DefaultValue,
                    EnumValues = enumValues
                });
            }

            _toolInfos[attr.Name] = new ToolInfo()
            {
                Name = attr.Name,
                Description = attr.Description,
                Parameters = parameters
            };

            _toolExecutioninfos[attr.Name] = new ToolExecutionInfo()
            {
                ToolInstance = tool,
                ParameterType = paramType
            };
        }

    }

    public IEnumerable<string> GetToolNames() =>
        _toolInfos.Keys;

    public ToolInfo? GetToolInfo(string toolName) =>
        _toolInfos.TryGetValue(toolName, out var toolInfo)
            ? toolInfo
            : null;
            
    public async Task<ToolResult> ExecuteTool(string toolName, JsonElement parameters)
    {
        if (!_toolExecutioninfos.TryGetValue(toolName, out var info))
            return ToolResult.Failure(ToolResultType.MalformedToolName, $"Tool '{toolName}' not found.");

        object? paramObj;
        try
        {
            paramObj = parameters.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                ? Activator.CreateInstance(info.ParameterType)
                : JsonSerializer.Deserialize(parameters, info.ParameterType);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(ToolResultType.MalformedArguments,
                $"Failed to deserialize parameters: {ex.Message}");
        }
        if (paramObj is null)
            return ToolResult.Failure(ToolResultType.MalformedArguments, "Param object resolved as null.");

        try
        {
            dynamic tool = info.ToolInstance;
            string? resultJson = await tool.InvokeAsync((dynamic)paramObj);

            return resultJson is not null
                ? ToolResult.Success(resultJson)
                : ToolResult.Failure(ToolResultType.ExecutionError, "Tool returned null result.");
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(ToolResultType.ExecutionError,
                $"Tool execution threw an exception: {ex.Message}");
        }
    }

}
