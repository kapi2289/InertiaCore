namespace InertiaCore.Utils;

public class OptionalProp : IgnoreFirstLoad
{
    private readonly Func<Task<object?>> _callback;

    public OptionalProp(Func<object?> callback) => _callback = async () => await Task.FromResult(callback());

    public OptionalProp(Func<Task<object?>> callback) => _callback = callback;

    public object? Invoke() => Task.Run(() => _callback.Invoke()).GetAwaiter().GetResult();
}
