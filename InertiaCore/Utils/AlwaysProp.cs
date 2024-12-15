namespace InertiaCore.Utils;

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
