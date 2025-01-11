using InertiaCore.Props;

namespace InertiaCore.Utils;

public class MergeProp : InvokableProp, Mergeable
{
    public bool merge { get; set; } = true;

    public MergeProp(object? value) : base(value)
    {
        merge = true;
    }

    internal MergeProp(Func<object?> value) : base(value)
    {
        merge = true;
    }

    internal MergeProp(Func<Task<object?>> value) : base(value)
    {
        merge = true;
    }
}


