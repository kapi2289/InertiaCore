namespace InertiaCore.Utils;

public class LazyProp
{
    private readonly Func<Task<object?>> _callback;

    public LazyProp(Func<object?> callback) => _callback = async () => await Task.FromResult(callback());
    public LazyProp(Func<Task<object?>> callback) => _callback = callback;

    public object? Invoke() => Task.Run(() => _callback.Invoke()).GetAwaiter().GetResult();
}
