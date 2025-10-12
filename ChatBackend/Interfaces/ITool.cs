namespace ChatBackend.Interfaces;

public interface ITool { }

public interface ITool<TParams> : ITool
{
    //Result as JSON string or null on error
    Task<string?> InvokeAsync(TParams parameters);
}
