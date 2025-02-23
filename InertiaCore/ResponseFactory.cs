using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaCore.Models;
using InertiaCore.Props;
using InertiaCore.Ssr;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InertiaCore;

internal interface IResponseFactory
{
    public Response Render(string component, object? props = null);
    public Task<IHtmlContent> Head(dynamic model);
    public Task<IHtmlContent> Html(dynamic model);
    public void Version(object? version);
    public string? GetVersion();
    public LocationResult Location(string url);
    public void Share(string key, object? value);
    public void Share(IDictionary<string, object?> data);
    public void ClearHistory(bool clear = true);
    public void EncryptHistory(bool encrypt = true);
    public AlwaysProp Always(object? value);
    public AlwaysProp Always(Func<object?> callback);
    public AlwaysProp Always(Func<Task<object?>> callback);
    public LazyProp Lazy(Func<object?> callback);
    public LazyProp Lazy(Func<Task<object?>> callback);
}

internal class ResponseFactory : IResponseFactory
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IGateway _gateway;
    private readonly IOptions<InertiaOptions> _options;

    private object? _version;
    private bool _clearHistory;
    private bool? _encryptHistory;

    public ResponseFactory(IHttpContextAccessor contextAccessor, IGateway gateway, IOptions<InertiaOptions> options) =>
        (_contextAccessor, _gateway, _options) = (contextAccessor, gateway, options);

    public Response Render(string component, object? props = null)
    {
        props ??= new { };
        var dictProps = props switch
        {
            Dictionary<string, object?> dict => dict,
            _ => props.GetType().GetProperties()
                .ToDictionary(o => o.Name, o => o.GetValue(props))
        };

        return new Response(component, dictProps, _options.Value.RootView, GetVersion(), _encryptHistory ?? _options.Value.EncryptHistory, _clearHistory);
    }

    public async Task<IHtmlContent> Head(dynamic model)
    {
        if (!_options.Value.SsrEnabled) return new HtmlString("");

        var context = _contextAccessor.HttpContext!;

        var response = context.Features.Get<SsrResponse>();
        response ??= await _gateway.Dispatch(model, _options.Value.SsrUrl);

        if (response == null) return new HtmlString("");

        context.Features.Set(response);
        return response.GetHead();
    }

    public async Task<IHtmlContent> Html(dynamic model)
    {
        if (_options.Value.SsrEnabled)
        {
            var context = _contextAccessor.HttpContext!;

            var response = context.Features.Get<SsrResponse>();
            response ??= await _gateway.Dispatch(model, _options.Value.SsrUrl);

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

        var encoded = WebUtility.HtmlEncode(data);

        return new HtmlString($"<div id=\"app\" data-page=\"{encoded}\"></div>");
    }

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

        var sharedData = context.Features.Get<InertiaSharedProps>();
        sharedData ??= new InertiaSharedProps();
        sharedData.Set(key, value);

        context.Features.Set(sharedData);
    }

    public void Share(IDictionary<string, object?> data)
    {
        var context = _contextAccessor.HttpContext!;

        var sharedData = context.Features.Get<InertiaSharedProps>();
        sharedData ??= new InertiaSharedProps();
        sharedData.Merge(data);

        context.Features.Set(sharedData);
    }

    public void ClearHistory(bool clear = true) => _clearHistory = clear;

    public void EncryptHistory(bool encrypt = true) => _encryptHistory = encrypt;

    public LazyProp Lazy(Func<object?> callback) => new(callback);
    public LazyProp Lazy(Func<Task<object?>> callback) => new(callback);
    public AlwaysProp Always(object? value) => new(value);
    public AlwaysProp Always(Func<object?> callback) => new(callback);
    public AlwaysProp Always(Func<Task<object?>> callback) => new(callback);
}
