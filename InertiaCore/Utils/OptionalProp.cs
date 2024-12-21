namespace InertiaCore.Utils;

public class OptionalProp : IgnoreFirstLoad
{
    private readonly object? _value;

    public OptionalProp(Func<object?> callback) => _value = callback;
    public OptionalProp(Func<Task<object?>> callback) => _value = callback;

    public object? Invoke()
    {
        // Check if the value is a callable delegate
        return Task.Run(async () =>
           {
               if (_value is Func<Task<object?>> asyncCallable)
               {
                   return await asyncCallable.Invoke();
               }

               if (_value is Func<object?> callable)
               {
                   return callable.Invoke();
               }

               return _value;
           }).GetAwaiter().GetResult();
    }
}
