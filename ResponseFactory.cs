using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Html;

namespace InertiaCore;

internal interface IResponseFactory
{
    public Response Render(string component, object? props = null);
    public IHtmlContent Html(dynamic model);
    public void SetRootView(string rootView);
    public void Version(object? version);
    public string? GetVersion();
    public LocationResult Location(string url);
}

internal class ResponseFactory : IResponseFactory
{
    private string _rootView = "~/Views/App.cshtml";

    private object? _version;

    public Response Render(string component, object? props = null)
    {
        props ??= new { };

        return new Response(component, props, _rootView, GetVersion());
    }

    public IHtmlContent Html(dynamic model)
    {
        var data = JsonSerializer.Serialize(model,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });

        var encoded = HttpUtility.HtmlEncode(data);

        return new HtmlString($"<div id=\"app\" data-page=\"{encoded}\"></div>");
    }

    public void SetRootView(string rootView) => _rootView = rootView;

    public void Version(object? version) => _version = version;

    public string? GetVersion() => _version switch
    {
        Func<string> func => func.Invoke(),
        string s => s,
        _ => null,
    };

    public LocationResult Location(string url) => new(url);
}
