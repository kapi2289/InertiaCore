namespace InertiaCore.Utils;

public class LazyProp
{
    private readonly Func<object?> _callback;
    private readonly bool _isAsync = false;

    public LazyProp(Func<object?> callback) => _callback = callback;

    public LazyProp(Func<Task<object?>> callback)
    {
        _callback = callback;
        _isAsync = true;
    }

    public object? Invoke() => (_isAsync == false) ? _callback.Invoke() : Task.Run(() => _callback.Invoke()).GetAwaiter().GetResult();
}
