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
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;

    private ActionContext? _context;
    private Page? _page;
    private IDictionary<string, object>? _viewData;

    internal Response(string component, Dictionary<string, object?> props, string rootView, string? version, bool encryptHistory, bool clearHistory)
        => (_component, _props, _rootView, _version, _encryptHistory, _clearHistory) = (component, props, rootView, version, encryptHistory, clearHistory);

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
            Props = props,
            EncryptHistory = _encryptHistory,
            ClearHistory = _clearHistory,
        };

        var mergeable = GetMergeablePropsForRequest();
        page.MergeProps = ResolveMergeProps(mergeable);
        page.PrependProps = ResolvePrependProps(mergeable);
        page.DeepMergeProps = ResolveDeepMergeProps(mergeable);
        page.MatchPropsOn = ResolveMatchPropsOn(mergeable);
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
    /// Get the props eligible for merging on this request.
    /// Mirrors Laravel's Response::getMergePropsForRequest():
    /// filters _props to only Mergeable && ShouldMerge(), removes any keys
    /// listed in the X-Inertia-Reset header, then applies Partial-Only /
    /// Partial-Except filtering.
    /// </summary>
    private Dictionary<string, object?> GetMergeablePropsForRequest(bool rejectResetProps = true)
    {
        var headers = _context!.HttpContext.Request.Headers;

        var resetKeys = rejectResetProps
            ? ParseHeaderList(headers[InertiaHeader.Reset].ToString())
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var hasPartialOnly = headers.ContainsKey(InertiaHeader.PartialOnly);
        var onlyKeys = hasPartialOnly
            ? ParseHeaderList(headers[InertiaHeader.PartialOnly].ToString())
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var exceptKeys = ParseHeaderList(headers[InertiaHeader.PartialExcept].ToString());

        var result = new Dictionary<string, object?>();
        foreach (var kv in _props)
        {
            if (kv.Value is not Mergeable m || !m.ShouldMerge()) continue;

            var camel = kv.Key.ToCamelCase();

            if (resetKeys.Contains(camel)) continue;
            if (hasPartialOnly && !onlyKeys.Contains(camel)) continue;
            if (exceptKeys.Contains(camel)) continue;

            result[kv.Key] = kv.Value;
        }

        return result;
    }

    private static HashSet<string> ParseHeaderList(string? value)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(value)) return set;

        foreach (var part in value!.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0) set.Add(trimmed);
        }
        return set;
    }

    /// <summary>
    /// Resolve merge props that should be appended (excludes deep merge and prepend props).
    /// Returns a flat list of prop keys or key.path entries.
    /// </summary>
    private static List<string>? ResolveMergeProps(Dictionary<string, object?> mergeProps)
    {
        var mergeableProps = mergeProps
            .Where(kv => kv.Value is Mergeable m && !m.ShouldDeepMerge())
            .ToList();

        if (mergeableProps.Count == 0) return null;

        var result = new List<string>();

        foreach (var kv in mergeableProps)
        {
            var m = (Mergeable)kv.Value!;
            var key = kv.Key.ToCamelCase();

            if (m.AppendsAtRoot())
            {
                result.Add(key);
            }

            foreach (var path in m.AppendsAtPaths)
            {
                result.Add($"{key}.{path}");
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Resolve props that should be prepended during merging.
    /// Returns a flat list of prop keys or key.path entries.
    /// </summary>
    private static List<string>? ResolvePrependProps(Dictionary<string, object?> mergeProps)
    {
        var mergeableProps = mergeProps
            .Where(kv => kv.Value is Mergeable m && !m.ShouldDeepMerge())
            .ToList();

        if (mergeableProps.Count == 0) return null;

        var result = new List<string>();

        foreach (var kv in mergeableProps)
        {
            var m = (Mergeable)kv.Value!;
            var key = kv.Key.ToCamelCase();

            if (m.PrependsAtRoot())
            {
                result.Add(key);
            }

            foreach (var path in m.PrependsAtPaths)
            {
                result.Add($"{key}.{path}");
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Resolve props that should be deep merged.
    /// </summary>
    private static List<string>? ResolveDeepMergeProps(Dictionary<string, object?> mergeProps)
    {
        var deepMergeProps = mergeProps
            .Where(kv => kv.Value is Mergeable m && m.ShouldDeepMerge())
            .Select(kv => kv.Key.ToCamelCase())
            .ToList();

        return deepMergeProps.Count > 0 ? deepMergeProps : null;
    }

    /// <summary>
    /// Resolve the match-on keys for merge props as a flat list.
    /// Returns entries like "propKey.strategy" matching Laravel's format.
    /// </summary>
    private static List<string>? ResolveMatchPropsOn(Dictionary<string, object?> mergeProps)
    {
        var result = new List<string>();

        foreach (var kv in mergeProps)
        {
            if (kv.Value is not Mergeable m) continue;

            var matchOnKeys = m.GetMatchOn();
            if (matchOnKeys == null || matchOnKeys.Length == 0) continue;

            var key = kv.Key.ToCamelCase();
            foreach (var matchOnItem in matchOnKeys)
            {
                result.Add($"{key}.{matchOnItem}");
            }
        }

        return result.Count > 0 ? result : null;
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
        _context!.HttpContext.Response.Headers.Override("Vary", InertiaHeader.Inertia);
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
