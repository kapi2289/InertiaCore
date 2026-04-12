using System.Text.Json;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

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

    internal static string MD5(this string s)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(s);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var t in hashBytes)
            sb.Append(t.ToString("x2"));

        return sb.ToString();
    }

    /// <summary>
    /// Gets the TempData dictionary for the current HTTP context.
    /// </summary>
    internal static ITempDataDictionary? GetTempData(this HttpContext context)
    {
        try
        {
            var tempDataFactory = context.RequestServices?.GetRequiredService<ITempDataDictionaryFactory>();
            return tempDataFactory?.GetTempData(context);
        }
        catch (InvalidOperationException)
        {
            // Service provider not available, return null
            return null;
        }
    }

    /// <summary>
    /// Sets validation errors in TempData for the specified error bag.
    /// </summary>
    public static void SetValidationErrors(this ITempDataDictionary tempData, Dictionary<string, string> errors,
        string bagName = "default")
    {
        // Deserialize existing error bags from JSON
        var errorBags = new Dictionary<string, Dictionary<string, string>>();
        if (tempData["__ValidationErrors"] is string existingJson && !string.IsNullOrEmpty(existingJson))
        {
            try
            {
                errorBags = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(existingJson)
                            ?? new Dictionary<string, Dictionary<string, string>>();
            }
            catch (JsonException)
            {
                // If deserialization fails, start fresh
                errorBags = new Dictionary<string, Dictionary<string, string>>();
            }
        }

        errorBags[bagName] = errors;

        // Serialize back to JSON for storage
        tempData["__ValidationErrors"] = JsonSerializer.Serialize(errorBags);
    }

    /// <summary>
    /// Sets validation errors in TempData from ModelState for the specified error bag.
    /// </summary>
    public static void SetValidationErrors(this ITempDataDictionary tempData, ModelStateDictionary modelState,
        string bagName = "default")
    {
        var errors = modelState.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? ""
        );
        tempData.SetValidationErrors(errors, bagName);
    }

    /// <summary>
    /// Retrieve and clear validation errors from TempData, supporting error bags.
    /// </summary>
    public static Dictionary<string, string> GetAndClearValidationErrors(this ITempDataDictionary tempData,
        HttpRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (!tempData.ContainsKey("__ValidationErrors"))
            return errors;

        // Deserialize from JSON
        Dictionary<string, Dictionary<string, string>> storedErrors;
        if (tempData["__ValidationErrors"] is string jsonString && !string.IsNullOrEmpty(jsonString))
        {
            try
            {
                storedErrors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonString) ??
                               new Dictionary<string, Dictionary<string, string>>();
            }
            catch (JsonException)
            {
                // If deserialization fails, return empty
                return errors;
            }
        }
        else
        {
            return errors;
        }

        // Check if there's a specific error bag in the request header
        var errorBag = "default";
        if (request.Headers.ContainsKey(InertiaHeader.ErrorBag))
        {
            errorBag = request.Headers[InertiaHeader.ErrorBag].ToString();
        }

        // If there's only the default bag and no specific bag requested, return the default bag directly
        if (storedErrors.Count == 1 && storedErrors.ContainsKey("default") && errorBag == "default")
        {
            foreach (var kvp in storedErrors["default"])
            {
                errors[kvp.Key] = kvp.Value;
            }
        }

        // If there are multiple bags or a specific bag is requested, return the named bag
        else if (storedErrors.ContainsKey(errorBag))
        {
            foreach (var kvp in storedErrors[errorBag])
            {
                errors[kvp.Key] = kvp.Value;
            }
        }

        // If no specific bag and multiple bags exist, return all bags
        else if (errorBag == "default" && storedErrors.Count > 1)
        {
            // Return all error bags as nested structure
            // This will be handled differently but for now just return default or first available
            var firstBag = storedErrors.Values.FirstOrDefault();
            if (firstBag != null)
            {
                foreach (var kvp in firstBag)
                {
                    errors[kvp.Key] = kvp.Value;
                }
            }
        }

        // Clear the temp data after reading (one-time use)
        tempData.Remove("__ValidationErrors");

        return errors;
    }
}
