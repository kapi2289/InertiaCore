using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace InertiaCore;

internal interface IResponseFactory
{
    public Response Render(string component, object? props = null);
    public Task<IHtmlContent> Head(dynamic model);
    public Task<IHtmlContent> Html(dynamic model);
    public void SetRootView(string rootView);
    public void Version(object? version);
    public string? GetVersion();
    public LocationResult Location(string url);
    public void Share(string key, object? value);
    public void Share(IDictionary<string, object?> data);
    public void EnableSsr(string? url = null);
}

internal class ResponseFactory : IResponseFactory
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IGateway _gateway;

    private string _rootView = "~/Views/App.cshtml";
    private object? _version;

    private bool _ssrEnabled;
    private string _ssrUrl = "http://127.0.0.1:13714/render";

    public ResponseFactory(IHttpContextAccessor contextAccessor, IGateway gateway) =>
        (_contextAccessor, _gateway) = (contextAccessor, gateway);

    public Response Render(string component, object? props = null)
    {
        props ??= new { };

        return new Response(component, props, _rootView, GetVersion());
    }

    public async Task<IHtmlContent> Head(dynamic model)
    {
        if (!_ssrEnabled) return new HtmlString("");

        var context = _contextAccessor.HttpContext!;

        var response = context.Features.Get<SsrResponse>();
        response ??= await _gateway.Dispatch(model, _ssrUrl);

        if (response == null) return new HtmlString("");

        context.Features.Set(response);
        return response.GetHead();
    }

    public async Task<IHtmlContent> Html(dynamic model)
    {
        if (_ssrEnabled)
        {
            var context = _contextAccessor.HttpContext!;

            var response = context.Features.Get<SsrResponse>();
            response ??= await _gateway.Dispatch(model, _ssrUrl);

            if (response != null)
            {
                context.Features.Set(response);
                return response.GetBody();
            }
        }

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

    public void EnableSsr(string? url)
    {
        _ssrEnabled = true;
        if (url != null)
            _ssrUrl = url;
    }
}
