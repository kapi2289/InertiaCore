using System.Text.Json;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCore.Extensions;

internal static class InertiaExtensions
{
    internal static Dictionary<string, object?> OnlyProps(this ActionContext context, Dictionary<string, object?> props)
    {
        var onlyKeys = context.HttpContext.Request.Headers[InertiaHeader.PartialOnly]
            .ToString().Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        return props.Where(kv => onlyKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    internal static Dictionary<string, object?> ExceptProps(this ActionContext context,
        Dictionary<string, object?> props)
    {
        var exceptKeys = context.HttpContext.Request.Headers[InertiaHeader.PartialExcept]
            .ToString().Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToList();

        return props.Where(kv => exceptKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) == false)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    internal static bool IsInertiaPartialComponent(this ActionContext context, string component) =>
        context.HttpContext.Request.Headers[InertiaHeader.PartialComponent] == component;

    internal static string RequestedUri(this HttpContext context) =>
        Uri.UnescapeDataString(context.Request.GetEncodedPathAndQuery());

    internal static string RequestedUri(this ActionContext context) => context.HttpContext.RequestedUri();

    internal static bool IsInertiaRequest(this HttpContext context) =>
        bool.TryParse(context.Request.Headers[InertiaHeader.Inertia], out _);

    internal static bool IsInertiaRequest(this ActionContext context) => context.HttpContext.IsInertiaRequest();

    internal static string ToCamelCase(this string s) => JsonNamingPolicy.CamelCase.ConvertName(s);

    internal static bool Override<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.TryAdd(key, value)) return false;
        dictionary[key] = value;

        return true;
    }
}
