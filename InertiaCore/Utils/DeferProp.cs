namespace InertiaCore.Utils;

public class DeferProp : IgnoreFirstLoad, Mergeable
{
    public bool merge { get; set; }

    private readonly object? _value;
    protected readonly string _group;

    public DeferProp(object? value, string group)
    {
        _value = value;
        _group = group;
    }

    public Mergeable Merge()
    {
        merge = true;

        return this;
    }

    public string? Group()
    {
        return _group;
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
