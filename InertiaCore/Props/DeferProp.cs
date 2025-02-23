using InertiaCore.Utils;

namespace InertiaCore.Props;

public class DeferProp : InvokableProp, IIgnoresFirstLoad, Mergeable
{
    public bool merge { get; set; }
    protected readonly string _group = "default";

    public DeferProp(object? value, string group) : base(value)
    {
        _group = group;
    }

    internal DeferProp(Func<object?> value, string group) : base(value)
    {
        _group = group;
    }

    internal DeferProp(Func<Task<object?>> value, string group) : base(value)
    {
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
}
