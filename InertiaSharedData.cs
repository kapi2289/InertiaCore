namespace InertiaCore;

public class InertiaSharedData
{
    public IDictionary<string, object?>? Data { get; set; }

    public Dictionary<string, object?> Merge(IDictionary<string, object?> with)
    {
        var result = new Dictionary<string, object?>();

        if (Data != null)
            foreach (var (key, value) in Data)
                result[key] = value;

        foreach (var (key, value) in with) result[key] = value;

        return result;
    }
}
