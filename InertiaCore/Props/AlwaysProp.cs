namespace InertiaCore.Props;

public class AlwaysProp
{
    private readonly object? _value;

    public AlwaysProp(object? value)
    {
        _value = value;
    }

    public object? Invoke()
    {
        // Check if the value is a callable delegate
        return Task.Run(async () =>
        {
            return _value switch
            {
                Func<Task<object?>> asyncCallable => await asyncCallable.Invoke(),
                Func<object?> callable => callable.Invoke(),
                _ => _value
            };
        }).GetAwaiter().GetResult();
    }
}
