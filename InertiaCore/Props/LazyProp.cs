using InertiaCore.Utils;

namespace InertiaCore.Props;

public class LazyProp : InvokableProp, IIgnoresFirstLoad
{
    internal LazyProp(Func<object?> value) : base(value)
    {
    }

    internal LazyProp(Func<Task<object?>> value) : base(value)
    {
    }
}
