namespace ChatBackend.Models;

public enum ToolResultType
{
    Success,
    MalformedToolName,
    MalformedArguments,
    ExecutionError
}
