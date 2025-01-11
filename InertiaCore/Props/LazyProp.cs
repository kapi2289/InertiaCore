namespace InertiaCore.Props;

public class LazyProp : InvokableProp
{
    internal LazyProp(Func<object?> value) : base(value)
    {
    }

    internal LazyProp(Func<Task<object?>> value) : base(value)
    {
    }
}
