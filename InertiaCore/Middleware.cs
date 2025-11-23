using System.Net;
using InertiaCore.Extensions;
using InertiaCore.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore;

public class Middleware
{
    private readonly RequestDelegate _next;

    public Middleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.IsInertiaRequest()
            && context.Request.Method == "GET"
            && context.Request.Headers[InertiaHeader.Version] != Inertia.GetVersion())
        {
            await OnVersionChange(context);
            return;
        }

        await _next(context);

        // Handle empty responses for Inertia requests
        if (context.IsInertiaRequest()
            && context.Response.StatusCode == 200
            && await IsEmptyResponse(context))
        {
            await OnEmptyResponse(context);
        }
    }

    private static async Task OnVersionChange(HttpContext context)
    {
        var tempData = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>()
            .GetTempData(context);

        if (tempData.Any()) tempData.Keep();

        context.Response.Headers.Override(InertiaHeader.Location, context.RequestedUri());
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;

        await context.Response.CompleteAsync();
    }

    private static async Task<bool> IsEmptyResponse(HttpContext context)
    {
        // Check if Content-Length is 0 or not set
        if (context.Response.Headers.ContentLength.HasValue)
        {
            return context.Response.Headers.ContentLength.Value == 0;
        }

        // Check if response body is empty or only whitespace
        if (context.Response.Body.CanSeek && context.Response.Body.Length >= 0)
        {
            var position = context.Response.Body.Position;

            // Check if the stream has any content
            if (context.Response.Body.Length == 0)
            {
                return true;
            }

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
            var content = await reader.ReadToEndAsync();

            context.Response.Body.Seek(position, SeekOrigin.Begin);

            return string.IsNullOrWhiteSpace(content);
        }

        // For non-seekable streams, check if the response body position is still 0
        // This indicates nothing has been written to the response
        try
        {
            return context.Response.Body.Position == 0;
        }
        catch
        {
            // If we can't determine, assume it's not empty to be safe
            return false;
        }
    }

    private static async Task OnEmptyResponse(HttpContext context)
    {
        // Use Inertia.Back() to redirect back
        var backResult = Inertia.Back();

        // Determine the redirect URL using the same logic as BackResult
        var referrer = context.Request.Headers.Referer.ToString();
        var redirectUrl = !string.IsNullOrEmpty(referrer) ? referrer : "/";

        // Set the appropriate headers and status code for a back redirect
        context.Response.StatusCode = (int)HttpStatusCode.SeeOther;
        context.Response.Headers.Override("Location", redirectUrl);

        await context.Response.CompleteAsync();
    }
}
