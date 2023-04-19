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

    // Set the filename for the manifest file.
    public static string? useManifestFilename(string newManifestFilename)
    {
        manifestFilename = newManifestFilename;
        return null;
    }

    // Set the Vite "hot" file path.
    public static string? useHotFile(string newHotFile)
    {
        hotFile = newHotFile;
        return null;
    }

    //  Set the Vite build directory.
    public static string? useBuildDir(string? newBuildDirectory)
    {
        buildDirectory = newBuildDirectory;
        return null;
    }

    //  Set the public directory.
    public static string? usePublicDir(string newPublicDirectory)
    {
        publicDirectory = newPublicDirectory;
        return null;
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

            return new HtmlString(makeTag(filePath.ToString()));
        }

        // Handle JS and CSS combo
        var html = makeTag(filePath.ToString());

        try
        {
            var css = obj.GetProperty("css");
            foreach (JsonElement item in css.EnumerateArray())
            {
                html = html + makeTag(item.ToString());
            }
        }
        catch (Exception)
        {

        }

        return new HtmlString(html);
    }

    private static string? makeModuleTag(string path)
    {
        var builder = new TagBuilder("script");
        builder.Attributes.Add("type", "module");
        builder.Attributes.Add("src", path);

        return new HtmlString(GetString(builder)).Value + "\n\t";
    }

    // Generate an appropriate tag for the given URL in HMR mode.
    private static string makeTag(string url)
    {
        if (isCssPath(url))
        {
            return makeStylesheetTag(url);
        }

        return makeScriptTag(url);
    }


    // Generate a script tag for the given URL.
    private static string makeScriptTag(string filePath)
    {
        var builder = new TagBuilder("script");
        builder.Attributes.Add("type", "text/javascript");
        builder.Attributes.Add("src", asset(filePath));
        return GetString(builder) + "\n\t";
    }

    // Generate a stylesheet tag for the given URL in HMR mode.
    private static string makeStylesheetTag(string filePath)
    {
        var builder = new TagBuilder("link");
        builder.Attributes.Add("rel", "stylesheet");
        builder.Attributes.Add("href", asset(filePath));
        return GetString(builder).Replace("></link>", " />") + "\n\t";
    }

    // Determine whether the given path is a CSS file.
    private static bool isCssPath(string path)
    {
        return Regex.IsMatch(path, @".\.(css|less|sass|scss|styl|stylus|pcss|postcss)", RegexOptions.IgnoreCase);
    }

    // Generate React refresh runtime script.
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

    // Get the path to a given asset when running in HMR mode.
    private static string hotAsset(string path)
    {
        var hotFilePath = getPublicDir(hotFile);
        var hotContents = File.ReadAllText(hotFilePath);

        return hotContents + "/" + path;

    }

    // Get the URL for an asset.
    public static string asset(string path)
    {
        if (isRunningHot())
        {
            return hotAsset(path);
        }

        var pieces = new List<string>();
        if (buildDirectory != null && buildDirectory != "")
        {
            pieces.Add(buildDirectory);
        }
        pieces.Add(path);
        return "/" + String.Join("/", pieces);
    }

    private static bool isRunningHot()
    {
        return File.Exists(getPublicDir("hot"));
    }

    private static string GetString(IHtmlContent content)
    {
        var writer = new System.IO.StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}

