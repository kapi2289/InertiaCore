namespace InertiaCore.Utils;

public interface Mergeable
{
    public bool merge { get; set; }

    public Mergeable Merge()
    {
        merge = true;

        return this;
    }

    public bool ShouldMerge() => merge;
}
