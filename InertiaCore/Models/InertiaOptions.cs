namespace InertiaCore.Models;

public class InertiaOptions
{
    public string RootView { get; set; } = "~/Views/App.cshtml";

    public bool SsrEnabled { get; set; } = false;
    public string SsrUrl { get; set; } = "http://127.0.0.1:13714/render";
    public bool EncryptHistory { get; set; } = false;

    public bool EnsurePagesExist { get; set; } = false;
    public string[] PagePaths { get; set; } = new[] { "~/ClientApp/src/Pages", "~/ClientApp/src/pages", "~/src/Pages", "~/src/pages" };
    public string[] PageExtensions { get; set; } = new[] { ".vue", ".svelte", ".js", ".jsx", ".ts", ".tsx" };
}
