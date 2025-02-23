using System.Text.Json.Serialization;

namespace InertiaCore.Models;

internal class Page
{
    public Dictionary<string, object?> Props { get; set; } = default!;
    public string Component { get; set; } = default!;
    public string? Version { get; set; }
    public string Url { get; set; } = default!;
    public bool EncryptHistory { get; set; } = false;
    public bool ClearHistory { get; set; } = false;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? MergeProps { get; set; }
}
