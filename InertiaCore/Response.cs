using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaCore.Extensions;
using InertiaCore.Models;
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
        ProcessResponse();

        await GetResult().ExecuteResultAsync(_context!);
    }

    protected internal void ProcessResponse()
    {
        var page = new Page
        {
            Component = _component,
            Version = _version,
            Url = _context!.RequestedUri(),
            Props = ResolveProperties(_props.GetType().GetProperties().ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props)))
        };

        var shared = _context!.HttpContext.Features.Get<InertiaSharedData>();
        if (shared != null)
            page.Props = shared.GetMerged(page.Props);

        page.Props["errors"] = GetErrors();

        SetPage(page);
    }

    private static Dictionary<string, object?> PrepareProps(Dictionary<string, object?> props)
    {
        return props.ToDictionary(pair => pair.Key, pair => pair.Value switch
        {
            Func<object?> f => f.Invoke(),
            LazyProp l => l.Invoke(),
            _ => pair.Value
        });
    }

    protected internal JsonResult GetJson()
    {
        _context!.HttpContext.Response.Headers.Add(Header.Inertia, "true");
        _context!.HttpContext.Response.Headers.Add("Vary", "Accept");
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

    private Dictionary<string, object?> ResolveProperties(Dictionary<string, object?> props)
    {
        bool isPartial = _context!.IsInertiaPartialComponent(_component);

        if (!isPartial)
        {
            props = props
                .Where(kv => kv.Value is not LazyProp)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        if (isPartial && _context!.HttpContext.Request.Headers.ContainsKey(Header.PartialOnly))
        {
            props = ResolveOnly(props);
        }

        if (isPartial && _context!.HttpContext.Request.Headers.ContainsKey(Header.PartialExcept))
        {
            props = ResolveExcept(props);
        }

        props = PrepareProps(props);

        return props;
    }

    private Dictionary<string, object?> ResolveOnly(Dictionary<string, object?> props)
    {
        return _context!.OnlyProps(props);
    }

    private Dictionary<string, object?> ResolveExcept(Dictionary<string, object?> props)
    {
        return _context!.ExceptProps(props);
    }
}
