using InertiaCore.Props;

namespace InertiaCore.Utils;

public class MergeProp : InvokableProp, Mergeable
{
    public bool merge { get; set; } = true;
    public bool deepMerge { get; set; } = false;
    public string[]? matchOn { get; set; }

    public bool Append { get; set; } = true;
    public List<string> AppendsAtPaths { get; } = new();
    public List<string> PrependsAtPaths { get; } = new();

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


