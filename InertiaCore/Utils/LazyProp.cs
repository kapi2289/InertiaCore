namespace InertiaCore.Utils;

public class LazyProp
{
    private readonly Func<object?> _callback;

    public LazyProp(Func<object?> callback) => _callback = callback;

    public object? Invoke() => _callback.Invoke();
}
