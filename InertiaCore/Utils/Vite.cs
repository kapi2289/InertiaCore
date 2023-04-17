using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InertiaCore.Utils;

public static class Vite
{

    private static string GetString(IHtmlContent content)
    {
        var writer = new System.IO.StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static string publicDir(string path)
    {
        return "wwwroot/build/" + path;
    }

    public static HtmlString input(string path)
    {
        if (isRunningHot())
        {
            return new HtmlString(makeModuleTag(hotAsset("@vite/client")) + makeModuleTag(hotAsset(path)));
        }

        if (!File.Exists(publicDir("manifest.json")))
        {
            throw new Exception("Vite Manifest is missing. Run `npm run build` and try again.");
        }

        var manifest = File.ReadAllText(publicDir("manifest.json"));
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

        if (filePath.ToString().EndsWith(".css"))
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

