namespace InertiaCore.Utils;

public interface ILazyProp
{
    object? Invoke();
}

public class LazyProp : ILazyProp
{
    private readonly Func<object?> _callback;

    public LazyProp(Func<object?> callback) => _callback = callback;


    public object? Invoke() => _callback.Invoke();
}

public class LazyPropAsync : ILazyProp
{
    private readonly Func<Task<object?>> _callback;
    public LazyPropAsync(Func<Task<object?>> callback) => _callback = callback;
    public object? Invoke() => Task.Run(() => _callback.Invoke()).GetAwaiter().GetResult();
}
