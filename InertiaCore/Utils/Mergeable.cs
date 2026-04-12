namespace InertiaCore.Utils;

public interface Mergeable
{
    public bool merge { get; set; }
    public bool deepMerge { get; set; }
    public string[]? matchOn { get; set; }

    public Mergeable Merge()
    {
        merge = true;

        return this;
    }

    public Mergeable DeepMerge()
    {
        deepMerge = true;

        merge = true;

        return this;
    }

    public Mergeable MatchesOn(params string[] keys)
    {
        matchOn = keys;
        return this;
    }

    public bool ShouldMerge() => merge;
    public bool ShouldDeepMerge() => deepMerge;
    public string[]? GetMatchOn() => matchOn;

    bool Append { get; set; }
    List<string> AppendsAtPaths { get; }
    List<string> PrependsAtPaths { get; }

    /// <summary>
    /// Specify that the value should be appended.
    /// </summary>
    public Mergeable AppendAt(string path, string? matchOnKey = null)
    {
        AppendsAtPaths.Add(path);
        if (matchOnKey != null)
        {
            var existing = matchOn?.ToList() ?? new List<string>();
            existing.Add($"{path}.{matchOnKey}");
            matchOn = existing.ToArray();
        }
        return this;
    }

    /// <summary>
    /// Specify that the value should be prepended.
    /// </summary>
    public Mergeable PrependAt(string path, string? matchOnKey = null)
    {
        PrependsAtPaths.Add(path);
        if (matchOnKey != null)
        {
            var existing = matchOn?.ToList() ?? new List<string>();
            existing.Add($"{path}.{matchOnKey}");
            matchOn = existing.ToArray();
        }
        return this;
    }

    /// <summary>
    /// Set the default merge direction to prepend.
    /// </summary>
    public Mergeable Prepend()
    {
        Append = false;
        merge = true;
        return this;
    }

    /// <summary>
    /// Whether this property appends at root level (no nested paths defined, default direction is append).
    /// </summary>
    public bool AppendsAtRoot() => Append && AppendsAtPaths.Count == 0 && PrependsAtPaths.Count == 0;

    /// <summary>
    /// Whether this property prepends at root level.
    /// </summary>
    public bool PrependsAtRoot() => !Append && AppendsAtPaths.Count == 0 && PrependsAtPaths.Count == 0;
}
