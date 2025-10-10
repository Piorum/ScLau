namespace ChatBackend.Interfaces;

public interface ITool<T>
{
    //Result as JSON string or null on error
    Task<string?> InvokeAsync(T parameters);
}
