using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace InertiaCore.Utils;

public static class Vite
{

    // The path to the "hot" file.
    private static string hotFile = "hot";

    // The path to the build directory.
    private static string? buildDirectory = "build";

    // The name of the manifest file.
    private static string manifestFilename = "manifest.json";

    // The path to the public directory.
    private static string publicDirectory = "wwwroot";
    public static string GetString(IHtmlContent content)
    {
        var writer = new System.IO.StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    // Set the filename for the manifest file.
    public static string useManifestFilename(string newManifestFilename)
    {
        return manifestFilename = newManifestFilename;
    }

    // Set the Vite "hot" file path.
    public static string useHotFile(string newHotFile)
    {
        return hotFile = newHotFile;
    }

    //  Set the Vite build directory.
    public static string? useBuildDir(string? newBuildDirectory)
    {
        return buildDirectory = newBuildDirectory;
    }

    //  Set the public directory.
    public static string usePublicDir(string newPublicDirectory)
    {
        return publicDirectory = newPublicDirectory;
    }

    //  Get the public directory and build path.
    private static string getPublicDir(string path)
    {
        var pieces = new List<string>();
        pieces.Add(publicDirectory);
        if (buildDirectory != null && buildDirectory != "")
        {
            pieces.Add(buildDirectory);
        }
        pieces.Add(path);
        return String.Join("/", pieces);
    }

    public static HtmlString input(string path)
    {
        if (isRunningHot())
        {
            return new HtmlString(makeModuleTag(hotAsset("@vite/client")) + makeModuleTag(hotAsset(path)));
        }

        if (!File.Exists(getPublicDir(manifestFilename)))
        {
            throw new Exception("Vite Manifest is missing. Run `npm run build` and try again.");
        }

        var manifest = File.ReadAllText(getPublicDir(manifestFilename));
        var manifestJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(manifest);

        if (manifestJson == null)
        {
            throw new Exception("Vite Manifest is invalid. Run `npm run build` and try again.");
        }

        if (!manifestJson.ContainsKey(path))
        {
            throw new Exception("Asset not found in manifest: " + path);
        }

        JsonElement obj = manifestJson[path];
        var filePath = obj.GetProperty("file");

        if (isCssPath(filePath.ToString()))
        {

            return new HtmlString(makeCssTag(filePath.ToString()));
        }

        // Handle JS and CSS combo
        var html = makeJsTag(filePath.ToString());

        try
        {
            var css = obj.GetProperty("css");
            foreach (JsonElement item in css.EnumerateArray())
            {
                html = html + makeCssTag(item.ToString());
            }
        }
        catch (Exception)
        {

        }

        return new HtmlString(html);
    }

    private static bool isCssPath(string path)
    {
        return Regex.IsMatch(path, @".\.(css|less|sass|scss|styl|stylus|pcss|postcss)", RegexOptions.IgnoreCase);
    }

    private static string makeCssTag(string filePath)
    {
        var builder = new TagBuilder("style");
        builder.Attributes.Add("type", "text/css");
        builder.InnerHtml.AppendHtml(File.ReadAllText(publicDir(filePath)));
        return GetString(builder);
    }

    private static string makeJsTag(string filePath)
    {
        var builder = new TagBuilder("script");
        builder.Attributes.Add("type", "text/javascript");

        var physicalPath = publicDir(filePath.ToString());
        if (File.Exists(physicalPath))
        {
            builder.InnerHtml.AppendHtml(File.ReadAllText(physicalPath));
        }

        return GetString(builder);
    }

    private static string hotAsset(string path)
    {
        var hotfile = publicDir("hot");
        var hotContents = File.ReadAllText(hotfile);

        return hotContents + "/" + path;
    }

    private static bool isRunningHot()
    {
        return File.Exists(publicDir("hot"));
    }

    private static string? makeModuleTag(string path)
    {
        var builder = new TagBuilder("script");
        builder.Attributes.Add("type", "module");


        builder.Attributes.Add("src", path);

        return new HtmlString(GetString(builder)).Value;
    }


    public static HtmlString reactRefresh()
    {

        if (!isRunningHot())
        {
            return new HtmlString("<!-- no hot -->");
        }

        var builder = new TagBuilder("script");
        builder.Attributes.Add("type", "module");

        var inner = String.Format(
            "import RefreshRuntime from '{0}';", hotAsset("@react-refresh")) +
            "RefreshRuntime.injectIntoGlobalHook(window);" +
            "window.$RefreshReg$ = () => { };" +
            "window.$RefreshSig$ = () => (type) => type;" +
            "window.__vite_plugin_react_preamble_installed__ = true;";


        builder.InnerHtml.AppendHtml(inner);

        return new HtmlString(GetString(builder));
    }
}

