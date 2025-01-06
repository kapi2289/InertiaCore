using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaCore.Extensions;
using InertiaCore.Models;
using InertiaCore.Props;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InertiaCore;

public class Response : IActionResult
{
    private readonly string _component;
    private readonly object _props;
    private readonly string _rootView;
    private readonly string? _version;

    private ActionContext? _context;
    private Page? _page;
    private IDictionary<string, object>? _viewData;

    public Response(string component, object props, string rootView, string? version)
        => (_component, _props, _rootView, _version) = (component, props, rootView, version);

    public async Task ExecuteResultAsync(ActionContext context)
    {
        SetContext(context);
        await ProcessResponse();

        await GetResult().ExecuteResultAsync(_context!);
    }

    protected internal async Task ProcessResponse()
    {
        var page = new Page
        {
            Component = _component,
            Version = _version,
            Url = _context!.RequestedUri(),
            Props = await ResolveProperties(_props.GetType().GetProperties()
                .ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props)))
        };

        var shared = _context!.HttpContext.Features.Get<InertiaSharedData>();
        if (shared != null)
            page.Props = shared.GetMerged(page.Props);

        page.Props["errors"] = GetErrors();

        SetPage(page);
    }

    private static async Task<Dictionary<string, object?>> PrepareProps(Dictionary<string, object?> props)
    {
        return (await Task.WhenAll(props.Select(async pair => pair.Value switch
        {
            Func<object?> f => (pair.Key, f.Invoke()),
            LazyProp l => (pair.Key, await l.Invoke()),
            AlwaysProp l => (pair.Key, await l.Invoke()),
            _ => (pair.Key, pair.Value)
        }))).ToDictionary(pair => pair.Key, pair => pair.Item2);
    }

    protected internal JsonResult GetJson()
    {
        _context!.HttpContext.Response.Headers.Override(InertiaHeader.Inertia, "true");
        _context!.HttpContext.Response.Headers.Override("Vary", "Accept");
        _context!.HttpContext.Response.StatusCode = 200;

        return new JsonResult(_page, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        });
    }

    private ViewResult GetView()
    {
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), _context!.ModelState)
        {
            Model = _page
        };

        if (_viewData == null) return new ViewResult { ViewName = _rootView, ViewData = viewData };

        foreach (var (key, value) in _viewData)
            viewData[key] = value;

        return new ViewResult { ViewName = _rootView, ViewData = viewData };
    }

    protected internal IActionResult GetResult() => _context!.IsInertiaRequest() ? GetJson() : GetView();

    private IDictionary<string, string> GetErrors()
    {
        if (!_context!.ModelState.IsValid)
            return _context!.ModelState.ToDictionary(o => o.Key.ToCamelCase(),
                o => o.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "");

        return new Dictionary<string, string>(0);
    }

    protected internal void SetContext(ActionContext context) => _context = context;

    private void SetPage(Page page) => _page = page;

    public Response WithViewData(IDictionary<string, object> viewData)
    {
        _viewData = viewData;
        return this;
    }

    private async Task<Dictionary<string, object?>> ResolveProperties(Dictionary<string, object?> props)
    {
        var isPartial = _context!.IsInertiaPartialComponent(_component);

        if (!isPartial)
        {
            props = props
                .Where(kv => kv.Value is not LazyProp)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        else
        {
            props = props.ToDictionary(kv => kv.Key, kv => kv.Value);

            if (_context!.HttpContext.Request.Headers.ContainsKey(InertiaHeader.PartialOnly))
                props = ResolveOnly(props);

            if (_context!.HttpContext.Request.Headers.ContainsKey(InertiaHeader.PartialExcept))
                props = ResolveExcept(props);
        }

        props = ResolveAlways(props);
        props = await PrepareProps(props);

        return props;
    }

    private Dictionary<string, object?> ResolveOnly(Dictionary<string, object?> props)
        => _context!.OnlyProps(props);

    private Dictionary<string, object?> ResolveExcept(Dictionary<string, object?> props)
        => _context!.ExceptProps(props);

    private Dictionary<string, object?> ResolveAlways(Dictionary<string, object?> props)
    {
        var alwaysProps = _props.GetType().GetProperties()
            .Where(o => o.PropertyType == typeof(AlwaysProp))
            .ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props));

        return props
            .Where(kv => kv.Value is not AlwaysProp)
            .Concat(alwaysProps).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
