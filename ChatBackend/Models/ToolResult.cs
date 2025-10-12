namespace ChatBackend.Models;

public class ToolResult
{
    public ToolResultType ResultType { get; init; }
    public string? Result { get; init; }
    public string? Error { get; init; }

    private ToolResult() {}

    public static ToolResult Success(string result) =>
        new() { ResultType = ToolResultType.Success, Result = result };

    public static ToolResult Failure(ToolResultType type, string? error = null) =>
        new() { ResultType = type, Error = error };

}
