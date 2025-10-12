namespace ChatBackend.Models;

public enum ToolResultType
{
    Success,
    MalformedToolName,
    MalformedParameters,
    ExecutionError
}
