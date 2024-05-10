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
            Url = _context!.RequestedUri()
        };

        var partial = _context!.GetPartialData();
        if (partial.Any() && _context!.IsInertiaPartialComponent(_component))
        {
            var only = _props.Only(partial);
            var partialProps = only.ToDictionary(o => o.ToCamelCase(), o =>
                _props.GetType().GetProperty(o)?.GetValue(_props));

            page.Props = partialProps;
        }
        else
        {
            var props = _props.GetType().GetProperties()
                .Where(o => o.PropertyType != typeof(ILazyProp))
                .ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props));

            page.Props = props;
        }

        page.Props = PrepareProps(page.Props);

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
            ILazyProp l => l.Invoke(),
            _ => pair.Value
        });
    }

    protected internal JsonResult GetJson()
    {
        _context!.HttpContext.Response.Headers.Add("X-Inertia", "true");
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
}
