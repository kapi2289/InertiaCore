using InertiaCore.Utils;

namespace InertiaCore.Props;

public class LazyProp : IgnoreFirstLoad
{
    private readonly object? _value;

    public LazyProp(Func<object?> callback) => _value = callback;
    public LazyProp(Func<Task<object?>> callback) => _value = callback;

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
