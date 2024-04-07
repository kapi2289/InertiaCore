namespace InertiaCore.Utils;

public class DeferProp : LazyProp
{
    public DeferProp(Func<object?> callback) : base(callback)
    {
    }
}
