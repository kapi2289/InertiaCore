using InertiaCore.Utils;

namespace InertiaCore.Props;

public class DeepMergeProp : InvokableProp, Mergeable
{
    public bool merge { get; set; } = true;
    public string[]? matchOn { get; set; }
    public bool deepMerge { get; set; } = true;
    public bool Append { get; set; } = true;
    public List<string> AppendsAtPaths { get; } = new();
    public List<string> PrependsAtPaths { get; } = new();

    public DeepMergeProp(object? value) : base(value)
    {
        merge = true;
        deepMerge = true;
    }

    internal DeepMergeProp(Func<object?> value) : base(value)
    {
        merge = true;
        deepMerge = true;
    }

    internal DeepMergeProp(Func<Task<object?>> value) : base(value)
    {
        merge = true;
        deepMerge = true;
    }

    public bool ShouldDeepMerge() => deepMerge;
}