using System.Text.Json;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCore.Extensions;

internal static class InertiaExtensions
{
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

    internal static Task<object?> ResolveAsync(this Func<object?> func)
    {
        var rt = func.Method.ReturnType;

        if (!rt.IsGenericType || rt.GetGenericTypeDefinition() != typeof(Task<>))
            return Task.Run(func.Invoke);

        var task = func.DynamicInvoke() as Task;
        return task!.ResolveResult();
    }

    internal static async Task<object?> ResolveResult(this Task task)
    {
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result");

        return result?.GetValue(task);
    }
}
