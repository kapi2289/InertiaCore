using System.Net;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace InertiaCore.Utils;

public class LocationResult : IActionResult
{
    private readonly string _url;

    public LocationResult(string url) => _url = url;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        if (context.IsInertiaRequest())
        {
            context.HttpContext.Response.Headers.Override(InertiaHeader.Location, _url);
            await new StatusCodeResult((int)HttpStatusCode.Conflict).ExecuteResultAsync(context);
            return;
        }

        await new RedirectResult(_url).ExecuteResultAsync(context);
    }
}
