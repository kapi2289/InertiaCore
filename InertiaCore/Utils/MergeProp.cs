namespace InertiaCore.Utils;

public class MergeProp : Mergeable
{
    public bool merge { get; set; } = true;

    private readonly object? _value;

    public MergeProp(object? value)
    {
        _value = value;
        merge = true;
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
