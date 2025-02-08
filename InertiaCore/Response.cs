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
    private readonly Dictionary<string, object?> _props;
    private readonly string _rootView;
    private readonly string? _version;

    private ActionContext? _context;
    private Page? _page;
    private IDictionary<string, object>? _viewData;

    internal Response(string component, Dictionary<string, object?> props, string rootView, string? version)
        => (_component, _props, _rootView, _version) = (component, props, rootView, version);

    public async Task ExecuteResultAsync(ActionContext context)
    {
        SetContext(context);
        await ProcessResponse();
        await GetResult().ExecuteResultAsync(_context!);
    }

    protected internal async Task ProcessResponse()
    {
        var props = await ResolveProperties();

        var page = new Page
        {
            Component = _component,
            Version = _version,
            Url = _context!.RequestedUri(),
            Props = props
        };

        page.Props["errors"] = GetErrors();

        SetPage(page);
    }

    /// <summary>
    /// Resolve the properties for the response.
    /// </summary>
    private async Task<Dictionary<string, object?>> ResolveProperties()
    {
        var props = _props;

        props = ResolveSharedProps(props);
        props = ResolvePartialProperties(props);
        props = ResolveAlways(props);
        props = await ResolvePropertyInstances(props);

        return props;
    }

    /// <summary>
    /// Resolve `shared` props stored in the current request context.
    /// </summary>
    private Dictionary<string, object?> ResolveSharedProps(Dictionary<string, object?> props)
    {
        var shared = _context!.HttpContext.Features.Get<InertiaSharedProps>();
        if (shared != null)
            props = shared.GetMerged(props);

        return props;
    }

    /// <summary>
    /// Resolve the `only` and `except` partial request props.
    /// </summary>
    private Dictionary<string, object?> ResolvePartialProperties(Dictionary<string, object?> props)
    {
        var isPartial = _context!.IsInertiaPartialComponent(_component);

        if (!isPartial)
            return props
                .Where(kv => kv.Value is not IIgnoresFirstLoad)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

        props = props.ToDictionary(kv => kv.Key, kv => kv.Value);

        if (_context!.HttpContext.Request.Headers.ContainsKey(InertiaHeader.PartialOnly))
            props = ResolveOnly(props);

        if (_context!.HttpContext.Request.Headers.ContainsKey(InertiaHeader.PartialExcept))
            props = ResolveExcept(props);

        return props;
    }

    /// <summary>
    /// Resolve the `only` partial request props.
    /// </summary>
    private Dictionary<string, object?> ResolveOnly(Dictionary<string, object?> props)
    {
        var onlyKeys = _context!.HttpContext.Request.Headers[InertiaHeader.PartialOnly]
            .ToString().Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        return props.Where(kv => onlyKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Resolve the `except` partial request props.
    /// </summary>
    private Dictionary<string, object?> ResolveExcept(Dictionary<string, object?> props)
    {
        var exceptKeys = _context!.HttpContext.Request.Headers[InertiaHeader.PartialExcept]
            .ToString().Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        return props.Where(kv => exceptKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) == false)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Resolve `always` properties that should always be included on all visits, regardless of "only" or "except" requests.
    /// </summary>
    private Dictionary<string, object?> ResolveAlways(Dictionary<string, object?> props)
    {
        var alwaysProps = _props.Where(o => o.Value is AlwaysProp);

        return props
            .Where(kv => kv.Value is not AlwaysProp)
            .Concat(alwaysProps).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Resolve all necessary class instances in the given props.
    /// </summary>
    private static async Task<Dictionary<string, object?>> ResolvePropertyInstances(Dictionary<string, object?> props)
    {
        return (await Task.WhenAll(props.Select(async pair =>
        {
            var key = pair.Key.ToCamelCase();

            var value = pair.Value switch
            {
                Func<object?> f => (key, await f.ResolveAsync()),
                Task t => (key, await t.ResolveResult()),
                InvokableProp p => (key, await p.Invoke()),
                _ => (key, pair.Value)
            };

            if (value.Item2 is Dictionary<string, object?> dict)
            {
                value = (key, await ResolvePropertyInstances(dict));
            }

            return value;
        }))).ToDictionary(pair => pair.key, pair => pair.Item2);
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

    private Dictionary<string, string> GetErrors()
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
}
