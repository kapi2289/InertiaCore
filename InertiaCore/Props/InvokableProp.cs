using InertiaCore.Extensions;

namespace InertiaCore.Props;

public class InvokableProp
{
    private readonly object? _value;

    protected InvokableProp(object? value) => _value = value;

    internal Task<object?> Invoke()
    {
        return _value switch
        {
            Func<object?> f => f.ResolveAsync(),
            Task t => t.ResolveResult(),
            InvokableProp p => p.Invoke(),
            _ => Task.FromResult(_value)
        };
    }
}
