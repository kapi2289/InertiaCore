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
        return Task.Run(() =>
           {
               if (_value is Delegate callable)
               {
                   return callable.DynamicInvoke();
               }

               return _value;
           }).GetAwaiter().GetResult();
    }
}
