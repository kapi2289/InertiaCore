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

        page.MergeProps = ResolveMergeProps(page.Props);
        page.DeferredProps = ResolveDeferredProps(page.Props);

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
            OptionalProp o => o.Invoke(),
            AlwaysProp a => a.Invoke(),
            MergeProp m => m.Invoke(),
            DeferProp d => d.Invoke(),
            _ => pair.Value
        });
    }

    protected internal JsonResult GetJson()
    {
        _context!.HttpContext.Response.Headers.Override(Header.Inertia, "true");
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

    private Dictionary<string, object?> ResolveProperties(Dictionary<string, object?> props)
    {
        bool isPartial = _context!.IsInertiaPartialComponent(_component);

        if (!isPartial)
        {
            props = props
                .Where(kv => (kv.Value as IgnoreFirstLoad) == null)
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

        props = ResolveAlways(props);

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

    private Dictionary<string, object?> ResolveAlways(Dictionary<string, object?> props)
    {
        var alwaysProps = _props.GetType().GetProperties()
                        .Where(o => o.PropertyType == typeof(AlwaysProp))
                        .ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props)); ;

        return props
            .Where(kv => kv.Value is not AlwaysProp)
            .Concat(alwaysProps).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private List<string>? ResolveMergeProps(Dictionary<string, object?> props)
    {
        // Parse the "RESET" header into a collection of keys to reset
        var resetProps = new HashSet<string>(
           _context!.HttpContext.Request.Headers[Header.Reset]
               .ToString()
               .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(s => s.Trim()),
           StringComparer.OrdinalIgnoreCase
       );

        var resolvedProps = props
            .Select(kv => kv.Key.ToCamelCase()) // Convert property name to camelCase
            .ToList();

        // Filter the props that are Mergeable and should be merged
        var mergeProps = _props.GetType().GetProperties().ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props))
            .Where(kv => kv.Value is Mergeable mergeable && mergeable.ShouldMerge()) // Check if value is Mergeable and should merge
            .Where(kv => !resetProps.Contains(kv.Key)) // Exclude reset keys
            .Select(kv => kv.Key.ToCamelCase()) // Convert property name to camelCase
            .Where(resolvedProps.Contains) // Filter only the props that are in the resolved props
            .ToList();

        if (mergeProps.Count == 0)
        {
            return null;
        }

        // Return the result
        return mergeProps;
    }

    private Dictionary<string, List<string>>? ResolveDeferredProps(Dictionary<string, object?> props)
    {

        bool isPartial = _context!.IsInertiaPartialComponent(_component);
        if (isPartial)
        {
            return null;
        }

        var deferredProps = _props.GetType().GetProperties().ToDictionary(o => o.Name.ToCamelCase(), o => o.GetValue(_props))
            .Where(kv => kv.Value is DeferProp) // Filter props that are instances of DeferProp
            .Select(kv => new
            {
                Key = kv.Key,
                Group = ((DeferProp)kv.Value!).Group()
            }) // Map each prop to a new object with Key and Group

            .GroupBy(x => x.Group) // Group by 'Group'
            .ToDictionary(
                g => g.Key!,
                g => g.Select(x => x.Key).ToList() // Extract 'Key' for each group
            );

        if (deferredProps.Count == 0)
        {
            return null;
        }

        // Return the result
        return deferredProps;
    }
}
