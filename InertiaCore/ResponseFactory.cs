using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace InertiaCore;

internal interface IResponseFactory
{
    public Response Render(string component, object? props = null);
    public IHtmlContent Html(dynamic model);
    public void SetRootView(string rootView);
    public void Version(object? version);
    public string? GetVersion();
    public LocationResult Location(string url);
    public void Share(string key, object? value);
    public void Share(IDictionary<string, object?> data);
}

internal class ResponseFactory : IResponseFactory
{
    private readonly IHttpContextAccessor _contextAccessor;

    private string _rootView = "~/Views/App.cshtml";
    private object? _version;

    public ResponseFactory(IHttpContextAccessor contextAccessor) => _contextAccessor = contextAccessor;

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

    public void Share(string key, object? value)
    {
        var context = _contextAccessor.HttpContext!;

        var sharedData = context.Features.Get<InertiaSharedData>();
        sharedData ??= new InertiaSharedData();
        sharedData.Set(key, value);

        context.Features.Set(sharedData);
    }

    public void Share(IDictionary<string, object?> data)
    {
        var context = _contextAccessor.HttpContext!;

        var sharedData = context.Features.Get<InertiaSharedData>();
        sharedData ??= new InertiaSharedData();
        sharedData.Merge(data);

        context.Features.Set(sharedData);
    }
}
