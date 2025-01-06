using InertiaCore.Utils;

namespace InertiaCore.Props;

public class OptionalProp : IIgnoresFirstLoad
{
    private readonly object? _value;

    public OptionalProp(Func<object?> callback) => _value = callback;
    public OptionalProp(Func<Task<object?>> callback) => _value = callback;

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
