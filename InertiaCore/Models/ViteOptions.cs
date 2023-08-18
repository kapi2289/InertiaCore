namespace InertiaCore.Models;

public class ViteOptions
{
    // The path to the "hot" file.
    public string HotFile { get; set; } = "hot";

    // The path to the build directory.
    public string? BuildDirectory { get; set; } = "build";

    // The name of the manifest file.
    public string ManifestFilename { get; set; } = "manifest.json";

    // The path to the public directory.
    public string PublicDirectory { get; set; } = "wwwroot";
}
