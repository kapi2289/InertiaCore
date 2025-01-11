using InertiaCore.Utils;

namespace InertiaCore.Props;

public class OptionalProp : InvokableProp, IIgnoresFirstLoad
{
    internal OptionalProp(Func<object?> value) : base(value)
    {
    }

    internal OptionalProp(Func<Task<object?>> value) : base(value)
    {
    }
}
