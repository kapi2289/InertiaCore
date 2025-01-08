namespace InertiaCore.Props;

public class InvokableProp
{
    private readonly object? _value;

    protected InvokableProp(object? value) => _value = value;

    internal Task<object?> Invoke()
    {
        return _value switch
        {
            Func<Task<object?>> asyncCallable => asyncCallable.Invoke(),
            Func<object?> callable => Task.Run(() => callable.Invoke()),
            Task<object?> value => value,
            _ => Task.FromResult(_value)
        };
    }
}
