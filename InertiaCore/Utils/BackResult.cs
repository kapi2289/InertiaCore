using System.Net;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Utils;

public class BackResult : IActionResult
{
    private readonly string _fallbackUrl;
    private readonly HttpStatusCode _statusCode;

    public BackResult(string? fallbackUrl = null, HttpStatusCode statusCode = HttpStatusCode.SeeOther) =>
        (_fallbackUrl, _statusCode) = (fallbackUrl ?? "/", statusCode);

    public Task ExecuteResultAsync(ActionContext context)
    {
        // Store validation errors in TempData if ModelState has errors
        if (!context.ModelState.IsValid)
        {
            var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context.HttpContext);
            tempData.SetValidationErrors(context.ModelState);
        }

        var referrer = context.HttpContext.Request.Headers.Referer.ToString();
        var redirectUrl = !string.IsNullOrEmpty(referrer) ? referrer : _fallbackUrl;

        context.HttpContext.Response.StatusCode = (int)_statusCode;
        context.HttpContext.Response.Headers.Location = redirectUrl;

        return Task.CompletedTask;
    }
}
